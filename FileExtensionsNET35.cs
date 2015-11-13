using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace Communary
{
	internal sealed class Win32Native
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern SafeFindHandle FindFirstFileExW(
			string lpFileName,
			FINDEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FIND_DATAW lpFindFileData,
			FINDEX_SEARCH_OPS fSearchOp,
			IntPtr lpSearchFilter,
			FINDEX_ADDITIONAL_FLAGS dwAdditionalFlags);
	
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		public static extern bool FindNextFile(SafeFindHandle hFindFile, out WIN32_FIND_DATAW lpFindFileData);
	
		[DllImport("kernel32.dll")]
		public static extern bool FindClose(IntPtr hFindFile);
	
		[DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
		public static extern bool PathMatchSpec([In] String pszFileParam, [In] String pszSpec);
	
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool GetDiskFreeSpace(
			string lpRootPathName,
			out uint lpSectorsPerCluster,
			out uint lpBytesPerSector,
			out uint lpNumberOfFreeClusters,
			out uint lpTotalNumberOfClusters);
	
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
		
		[DllImport("kernel32.dll")]
		public static extern bool SetFileAttributesW(
     		string lpFileName,
     		[MarshalAs(UnmanagedType.U4)] FileAttributes dwFileAttributes);
	
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WIN32_FIND_DATAW
		{
			public FileAttributes dwFileAttributes;
			internal System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
			internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
			internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public uint dwReserved0;
			public uint dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string cAlternateFileName;
		}
	
		public enum FINDEX_INFO_LEVELS
		{
			FindExInfoStandard,             // Return a standard set of attribute information.
			FindExInfoBasic,                // Does not return the short file name, improving overall enumeration speed. cAlternateFileName is always a NULL string.
			FindExInfoMaxInfoLevel          // This value is used for validation. Supported values are less than this value.
		}
	
		public enum FINDEX_SEARCH_OPS
		{
			FindExSearchNameMatch,          // The search for a file that matches a specified file name.
			FindExSearchLimitToDirectories, // This is an advisory flag. If the file system supports directory filtering, the function searches for a file that matches the specified name and is also a directory. If the file system does not support directory filtering, this flag is silently ignored.
			FindExSearchLimitToDevices      // This filtering type is not available.
		}
	
		[Flags]
		public enum FINDEX_ADDITIONAL_FLAGS
		{
			FindFirstExCaseSensitive,
			FindFirstExLargeFetch
		}
	}
	
	[SecurityCritical]
	internal class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		[SecurityCritical]
		public SafeFindHandle() : base(true)
		{ }
	
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.FindClose(base.handle);
		}
	}
	
	public static class FILETIMEExtensions
	{
		public static DateTime ToDateTime(this System.Runtime.InteropServices.ComTypes.FILETIME time)
		{
			ulong high = (ulong)time.dwHighDateTime;
			ulong low = (ulong)time.dwLowDateTime;
			long fileTime = (long)((high << 32) + low);
			return DateTime.FromFileTimeUtc(fileTime);
		}
	}
	
	public static class FileExtensions
	{
		// prefix for long path support
		private const string normalPrefix = @"\\?\";
		private const string uncPrefix = @"\\?\UNC\";
	
		public static uint GetSectorSize(string path)
		{
			// add prefix to allow for maximum path of up to 32,767 characters
			string prefixedPath;
			if (path.StartsWith(@"\\"))
			{
				prefixedPath = path.Replace(@"\\", uncPrefix);
			}
			else
			{
				prefixedPath = normalPrefix + path;
			}
	
			uint lpSectorsPerCluster;
			uint lpBytesPerSector;
			uint lpNumberOfFreeClusters;
			uint lpTotalNumberOfClusters;
	
			string pathRoot = Path.GetPathRoot(path);
			if (!pathRoot.EndsWith(@"\"))
			{
				pathRoot = pathRoot + @"\";
			}
	
			bool result = Win32Native.GetDiskFreeSpace(pathRoot, out lpSectorsPerCluster, out lpBytesPerSector, out lpNumberOfFreeClusters, out lpTotalNumberOfClusters);
			if (result)
			{
				uint clusterSize = lpSectorsPerCluster * lpBytesPerSector;
				return clusterSize;
			}
			else
			{
				return 0;
			}
		}
	
		public static void DeleteFile(string path)
		{
			string prefixedPath;
			if (path.StartsWith(@"\\"))
			{
				prefixedPath = path.Replace(@"\\", uncPrefix);
			}
			else
			{
				prefixedPath = normalPrefix + path;
			}
			try
			{
				Win32Native.DeleteFileW(prefixedPath);
			}
			catch
			{
				int hr = Marshal.GetLastWin32Error();
				if (hr != 2 && hr != 0x12)
				{
					throw new Win32Exception(hr);
					//Console.WriteLine("{0}:  {1}", path, (new Win32Exception(hr)).Message);
				}
			}
		}
	
		public static List<FileInformation> FastFind(string path, string searchPattern, bool getFile, bool getDirectory, bool recurse, int? depth, bool parallel, bool suppressErrors, bool largeFetch, bool getHidden, bool getSystem, bool getReadOnly, bool getCompressed, bool getArchive, bool getReparsePoint, string filterMode)
		{
			object resultListLock = new object();
			Win32Native.WIN32_FIND_DATAW lpFindFileData;
			Win32Native.FINDEX_ADDITIONAL_FLAGS additionalFlags = 0;
			if (largeFetch)
			{
				additionalFlags = Win32Native.FINDEX_ADDITIONAL_FLAGS.FindFirstExLargeFetch;
			}
	
			// add prefix to allow for maximum path of up to 32,767 characters
			string prefixedPath;
			if (path.StartsWith(@"\\"))
			{
				prefixedPath = path.Replace(@"\\", uncPrefix);
			}
			else
			{
				prefixedPath = normalPrefix + path;
			}
	
			var handle = Win32Native.FindFirstFileExW(prefixedPath + @"\*", Win32Native.FINDEX_INFO_LEVELS.FindExInfoBasic, out lpFindFileData, Win32Native.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
	
			List<FileInformation> resultList = new List<FileInformation>();
			List<FileInformation> subDirectoryList = new List<FileInformation>();
	
			if (!handle.IsInvalid)
			{
				do
				{
					// skip "." and ".."
					if (lpFindFileData.cFileName != "." && lpFindFileData.cFileName != "..")
					{
						// if directory...
						if ((lpFindFileData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
						{
							// ...and if we are performing a recursive search...
							if (recurse)
							{
								// ... populate the subdirectory list
								string fullName = Path.Combine(path, lpFindFileData.cFileName);
								subDirectoryList.Add(new FileInformation { Path = fullName });
							}
						}
	
						// skip folders if only the getFile parameter is used
						if (getFile && !getDirectory)
						{
							if ((lpFindFileData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
							{
								continue;
							}
						}
	
						// if file matches search pattern and attribute filter, add it to the result list
						if (MatchesFilter(lpFindFileData.dwFileAttributes, lpFindFileData.cFileName, searchPattern, getFile, getDirectory, getHidden, getSystem, getReadOnly, getCompressed, getArchive, getReparsePoint, filterMode))
						{
							string fullName = Path.Combine(path, lpFindFileData.cFileName);
							long? thisFileSize = null;
                            if ((lpFindFileData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                            {
                                thisFileSize = (lpFindFileData.nFileSizeHigh * (2 ^ 32) + lpFindFileData.nFileSizeLow);
                            }
							resultList.Add(new FileInformation { Name = lpFindFileData.cFileName, Path = Path.Combine(path, lpFindFileData.cFileName), Parent = path, Attributes = lpFindFileData.dwFileAttributes, FileSize = thisFileSize, CreationTime = lpFindFileData.ftCreationTime.ToDateTime(), LastAccessTime = lpFindFileData.ftLastAccessTime.ToDateTime(), LastWriteTime = lpFindFileData.ftLastWriteTime.ToDateTime() });
							
						}
					}
				}
				while (Win32Native.FindNextFile(handle, out lpFindFileData));
	
				// close the file handle
				handle.Dispose();
	
				// handle recursive search
				if (recurse)
				{
					// handle depth of recursion
					if (depth > 0)
					{	
						foreach (FileInformation directory in subDirectoryList)
						{
							foreach (FileInformation result in FastFind(directory.Path, searchPattern, getFile, getDirectory, recurse, (depth - 1), false, suppressErrors, largeFetch, getHidden, getSystem, getReadOnly, getCompressed, getArchive, getReparsePoint, filterMode))
							{
								resultList.Add(result);
							}
						}	
					}
	
					// if no depth are specified
					else if (depth == null)
					{
						foreach (FileInformation directory in subDirectoryList)
						{
							foreach (FileInformation result in FastFind(directory.Path, searchPattern, getFile, getDirectory, recurse, null, false, suppressErrors, largeFetch, getHidden, getSystem, getReadOnly, getCompressed, getArchive, getReparsePoint, filterMode))
							{
								resultList.Add(result);
							}
						}	
					}
				}
			}
	
			// error handling
			else if (handle.IsInvalid && !suppressErrors)
			{
				int hr = Marshal.GetLastWin32Error();
				if (hr != 2 && hr != 0x12)
				{
					//throw new Win32Exception(hr);
					Console.WriteLine("{0}:  {1}", path, (new Win32Exception(hr)).Message);
				}
			}
	
			return resultList;
		}
	
		private static bool MatchesFilter(FileAttributes fileAttributes, string name, string searchPattern, bool aFile, bool aDirectory, bool aHidden, bool aSystem, bool aReadOnly, bool aCompressed, bool aArchive, bool aReparsePoint, string filterMode)
		{
			// first make sure that the name matches the search pattern
			if (Win32Native.PathMatchSpec(name, searchPattern))
			{
				// then we build our filter attributes enumeration
				FileAttributes filterAttributes = new FileAttributes();
	
				if (aDirectory)
				{
					filterAttributes |= FileAttributes.Directory;
				}
	
				if (aHidden)
				{
					filterAttributes |= FileAttributes.Hidden;
				}
	
				if (aSystem)
				{
					filterAttributes |= FileAttributes.System;
				}
	
				if (aReadOnly)
				{
					filterAttributes |= FileAttributes.ReadOnly;
				}
	
				if (aCompressed)
				{
					filterAttributes |= FileAttributes.Compressed;
				}
	
				if (aReparsePoint)
				{
					filterAttributes |= FileAttributes.ReparsePoint;
				}
	
				if (aArchive)
				{
					filterAttributes |= FileAttributes.Archive;
				}
	
				// based on the filtermode, we match the file with our filter attributes a bit differently
				switch (filterMode)
				{
					case "Include":
						if ((fileAttributes & filterAttributes) == filterAttributes)
						{
							return true;
						}
						else
						{
							return false;
						}
					case "Exclude":
						if ((fileAttributes & filterAttributes) != filterAttributes)
						{
							return true;
						}
						else
						{
							return false;
						}
					case "Strict":
						if (fileAttributes == filterAttributes)
						{
							return true;
						}
						else
						{
							return false;
						}
				}
				return false;
			}
			else
			{
				return false;
			}
		}
	
		[Serializable]
		public class FileInformation
		{
			public string Name;
			public string Path;
			public string Parent;
			public FileAttributes Attributes;
			public long? FileSize;
			public DateTime CreationTime;
			public DateTime LastAccessTime;
			public DateTime LastWriteTime;
		}
	}
}
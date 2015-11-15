using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool RemoveDirectoryW(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetFileAttributesW(
             string lpFileName,
             [MarshalAs(UnmanagedType.U4)] FileAttributes dwFileAttributes);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetFileAttributesW(string lpFileName);
                
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint GetSecurityInfo(
            IntPtr hFindFile,
            SE_OBJECT_TYPE ObjectType,
            SECURITY_INFORMATION SecurityInfo,
            out IntPtr pSidOwner,
            out IntPtr pSidGroup,
            out IntPtr pDacl,
            out IntPtr pSacl,
            out IntPtr pSecurityDescriptor);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetNamedSecurityInfoW(
            string pObjectName,
            SE_OBJECT_TYPE ObjectType,
            SECURITY_INFORMATION SecurityInfo,
            out IntPtr pSidOwner,
            out IntPtr pSidGroup,
            out IntPtr pDacl,
            out IntPtr pSacl,
            out IntPtr pSecurityDescriptor);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint LookupAccountSid(
            string lpSystemName,
            IntPtr psid,
            StringBuilder lpName,
            ref uint cchName,
            [Out] StringBuilder lpReferencedDomainName,
            ref uint cchReferencedDomainName,
            out uint peUse);

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ConvertSidToStringSid(
        IntPtr sid,
        out IntPtr sidString);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LocalFree(
            IntPtr handle
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] FileAccess access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
            IntPtr templateFile);

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

        public enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
        }

        public enum SECURITY_INFORMATION
        {
            OWNER_SECURITY_INFORMATION = 1,     // The owner identifier of the object is being referenced. Right required to query: READ_CONTROL. Right required to set: WRITE_OWNER.
            GROUP_SECURITY_INFORMATION = 2,     // The primary group identifier of the object is being referenced. Right required to query: READ_CONTROL. Right required to set: WRITE_OWNER.
            DACL_SECURITY_INFORMATION = 4,      // The DACL of the object is being referenced. Right required to query: READ_CONTROL. Right required to set: WRITE_DAC.
            SACL_SECURITY_INFORMATION = 8,      // The SACL of the object is being referenced. Right required to query: ACCESS_SYSTEM_SECURITY. Right required to set: ACCESS_SYSTEM_SECURITY.
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

            bool success = Win32Native.DeleteFileW(prefixedPath);
            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }
		}

        public static void DeleteDirectory(string path)
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

            bool success = Win32Native.RemoveDirectoryW(prefixedPath);
            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }
        }

        public static void AddFileAttributes(string path, FileAttributes fileAttributes)
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

            bool success = Win32Native.SetFileAttributesW(prefixedPath, fileAttributes);
            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }
        }

        public static uint GetFileAttributes(string path)
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

            return (Win32Native.GetFileAttributesW(prefixedPath));
        }
                
        public static string GetFileOwner(string path)
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

            IntPtr NA = IntPtr.Zero;
            IntPtr sidOwner;

            var errorCode = Win32Native.GetNamedSecurityInfoW(prefixedPath, Win32Native.SE_OBJECT_TYPE.SE_FILE_OBJECT, Win32Native.SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, out sidOwner, out NA, out NA, out NA, out NA);
            if (errorCode == 0)
            {
                const uint bufferLength = 64;
                StringBuilder fileOwner = new StringBuilder();
                var accountLength = bufferLength;
                var domainLength = bufferLength;
                StringBuilder ownerAccount = new StringBuilder((int)bufferLength);
                StringBuilder ownerDomain = new StringBuilder((int)bufferLength);
                uint peUse;

                errorCode = Win32Native.LookupAccountSid(null, sidOwner, ownerAccount, ref accountLength, ownerDomain, ref domainLength, out peUse);
                if (errorCode != 0)
                {
                    fileOwner.Append(ownerDomain);
                    fileOwner.Append(@"\");
                    fileOwner.Append(ownerAccount);
                    return fileOwner.ToString();
                }
                else
                {
                    IntPtr sidString = IntPtr.Zero;
                    if (Win32Native.ConvertSidToStringSid(sidOwner, out sidString))
                    {
                        //string account = new System.Security.Principal.SecurityIdentifier(sidOwner).Translate(typeof(System.Security.Principal.NTAccount)).ToString();
                        //Console.WriteLine(account);
                        return Marshal.PtrToStringAuto(sidString);
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        throw new Win32Exception(lastError);
                    }
                }
            }
            else
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
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
						if (parallel)
						{
							subDirectoryList.AsParallel().ForAll(x =>
							{
								List<FileInformation> resultSubDirectory = new List<FileInformation>();
								resultSubDirectory = FastFind(x.Path, searchPattern, getFile, getDirectory, recurse, (depth - 1), false, suppressErrors, largeFetch, getHidden, getSystem, getReadOnly, getCompressed, getArchive, getReparsePoint, filterMode);
								lock (resultListLock)
								{
									resultList.AddRange(resultSubDirectory);
								}
							});
						}
	
						else
						{
							foreach (FileInformation directory in subDirectoryList)
							{
								foreach (FileInformation result in FastFind(directory.Path, searchPattern, getFile, getDirectory, recurse, (depth - 1), false, suppressErrors, largeFetch, getHidden, getSystem, getReadOnly, getCompressed, getArchive, getReparsePoint, filterMode))
								{
									resultList.Add(result);
								}
							}
						}
					}
	
					// if no depth are specified
					else if (depth == null)
					{
						if (parallel)
						{
							subDirectoryList.AsParallel().ForAll(x =>
							{
								List<FileInformation> resultSubDirectory = new List<FileInformation>();
								resultSubDirectory = FastFind(x.Path, searchPattern, getFile, getDirectory, recurse, null, false, suppressErrors, largeFetch, getHidden, getSystem, getReadOnly, getCompressed, getArchive, getReparsePoint, filterMode);
								lock (resultListLock)
								{
									resultList.AddRange(resultSubDirectory);
								}
							});
						}
	
						else
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

        [Serializable]
        public class SecurityInformation
        {
            public string Path;
            public string Owner;
            public string Access;
        }
	}
}
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace ARK_Server_Manager.Lib
{
    static class SecurityUtils
    {
        const string PasswordChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        static int callCount = 0;

        public static string GeneratePassword(int count)
        {
            StringBuilder newPassword = new StringBuilder(count);
            Random random;
            unchecked
            {
                random = new Random((int)DateTime.Now.Ticks + callCount);
                callCount++;
            }

            for(int i = 0; i < count; i++)
            {
                newPassword.Append(PasswordChars[random.Next(PasswordChars.Length)]);
            }

            return newPassword.ToString();
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return  principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool SetDirectoryOwnershipForAllUsers(string destinationDirectory)
        {
            const string USERSGROUP = @"BUILTIN\Users";

            try
            {
                var dirInfo = new DirectoryInfo(destinationDirectory);
                var security = dirInfo.GetAccessControl(AccessControlSections.Access);
                bool result;

                var iFlags = InheritanceFlags.None;

                // *** Add Access Rule to the actual directory itself
                var accessRule = new FileSystemAccessRule(USERSGROUP, FileSystemRights.FullControl, iFlags, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
                security.ModifyAccessRule(AccessControlModification.Set, accessRule, out result);

                if (!result)
                    return false;

                // *** Always allow objects to inherit on a directory
                iFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

                // *** Add Access rule for the inheritance
                accessRule = new FileSystemAccessRule(USERSGROUP, FileSystemRights.FullControl, iFlags, PropagationFlags.InheritOnly, AccessControlType.Allow);
                security.ModifyAccessRule(AccessControlModification.Add, accessRule, out result);

                if (!result)
                    return false;

                dirInfo.SetAccessControl(security);
            }
            catch (Exception)
            {
                // We give it a best-effort here.  If we aren't running an enterprise OS, this group may not exist (or be needed.)
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

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
    }
}

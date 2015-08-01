using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Controls;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class IpValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (IpValidationRule.ValidateIP((string)value))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "Invalid IP address or host name");
            }
        }

        private static bool ValidateIP(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }
            else
            {
                IPAddress ipAddress;
                if (IPAddress.TryParse(source, out ipAddress))
                {
                    return true;
                }
                else
                {
                    // Try DNS resolution
                    try
                    {
                        var addresses = Dns.GetHostAddresses(source);
                        var ip4Address = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        if (ip4Address != null)
                        {
                            Debug.WriteLine(String.Format("Resolved address {0} to {1}", source, ip4Address.ToString()));
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
        }

    }
}

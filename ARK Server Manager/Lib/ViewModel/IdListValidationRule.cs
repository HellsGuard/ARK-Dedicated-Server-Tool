using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Controls;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class IdListValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var strValue = (string)value;

            if (!String.IsNullOrWhiteSpace(strValue))
            {
                // check if there are any spaces
                if (strValue.Contains(" "))
                {
                    return new ValidationResult(false, "Spaces are not permitted");
                }

                // check for valid integers
                var entries = strValue.Split(',');

                int throwaway;
                if (entries.FirstOrDefault(e => !Int32.TryParse(e, out throwaway)) != null)
                {
                    return new ValidationResult(false, "Must be a comma-separated list of integers");
                }
            }

            return new ValidationResult(true, null);
        }
    }
}

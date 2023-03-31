using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PANGEA.IMPORTSUITE.WebApp.Models
{

    public class LoginModel
    {
        [Required]
        [Display(Name = "UserName")]
        public string UserName { get; set; }

        //[Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        //  [Required]
        //  [Display(Name = "Compañia")]
        //  public int Compania { get; set; }

        [Required]
        [Display(Name = "EsMobil")]
        public int EsMobil { get; set; }

        [Display(Name = "Remember Me?")]
        public bool RememberMe { get; set; }
    }


}

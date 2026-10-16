using System.ComponentModel.DataAnnotations;

public class UserValidator
{
   public static bool IsValid(User user)
   {
      var context = new ValidationContext(user);
      var results = new List<ValidationResult>();
      return Validator.TryValidateObject(user, context, results, true);
   }
}

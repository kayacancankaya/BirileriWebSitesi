using Microsoft.AspNetCore.Identity;

namespace BirileriWebSitesi.Areas
{
    public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
        new IdentityError { Code = nameof(DefaultError), Description = "Bilinmeyen bir hata oluştu." };
        public override IdentityError PasswordMismatch() =>
            new IdentityError { Code = nameof(PasswordMismatch), Description = $"Şifre mevcut şifre ile uyuşmuyor." };
        public override IdentityError InvalidToken() =>
            new IdentityError { Code = nameof(InvalidToken), Description = $"Oturum Doğrulanamadı." };
        public override IdentityError RecoveryCodeRedemptionFailed() =>
            new IdentityError { Code = nameof(RecoveryCodeRedemptionFailed), Description = $"Kurtarma Kodu Doğrulanamadı." };
        public override IdentityError LoginAlreadyAssociated() =>
            new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = $"Dış Kaynaklı Oturum Sistemde Halihazırda Mevcut." };
        public override IdentityError InvalidUserName(string? userName) =>
            new IdentityError { Code = nameof(InvalidUserName), Description = $"Kullanıcı Adı Geçerli Değil." };
        public override IdentityError InvalidEmail(string? email) =>
            new IdentityError { Code = nameof(InvalidEmail), Description = "Geçersiz e-posta adresi." };
        public override IdentityError DuplicateUserName(string? userName) =>
            new IdentityError { Code = nameof(DuplicateUserName), Description = $"Kullanıcı Adı {userName} Sistemde Kayıtlı. Yeni Bir Kullanıcı Adı Giriniz." };
        public override IdentityError DuplicateEmail(string? email) =>
            new IdentityError { Code = nameof(DuplicateEmail), Description = $"Email {email} Sistemde Kayıtlı. Yeni Bir Email Giriniz." };
        public override IdentityError InvalidRoleName(string? role) =>
            new IdentityError { Code = nameof(DuplicateEmail), Description = $"Görev {role} Sistemde Kayıtlı Değil. Sistemde Kayıtlı Bir Görev Seçiniz." };
        public override IdentityError DuplicateRoleName(string role) =>
            new IdentityError { Code = nameof(DuplicateRoleName), Description = $"Görev {role} Sistemde Halihazırda Kayıtlı. Yeni Bir Görev Giriniz." };
        public override IdentityError UserAlreadyHasPassword() =>
            new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = "Bu kullanıcı zaten bir parola belirlemiş." };
        public override IdentityError UserLockoutNotEnabled() =>
            new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = "Hesabınız geçici olarak kilitlenmiştir. Lütfen birkaç dakika sonra tekrar deneyin." };
        public override IdentityError UserAlreadyInRole(string role) =>
            new IdentityError { Code = nameof(UserAlreadyInRole), Description = $"Kullanıcıya halihazırda {role} tanımlı." };
        public override IdentityError UserNotInRole(string role) =>
            new IdentityError { Code = nameof(UserNotInRole), Description = $"Kullanıcıya {role} tanımlı değil." };
        public override IdentityError PasswordTooShort(int length) =>
            new IdentityError { Code = nameof(PasswordTooShort), Description = $"Parola en az {length} karakter olmalıdır." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = $"Parola {uniqueChars} benzersiz karakter içermelidir." };
        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Parola en az bir özel karakter içermelidir." };
        public override IdentityError PasswordRequiresDigit() =>
            new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "Parola en az bir rakam içermelidir." };
        public override IdentityError PasswordRequiresLower() =>
            new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Parola en az bir küçük harf içermelidir." };
        public override IdentityError PasswordRequiresUpper() =>
            new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Parola en az bir büyük harf içermelidir." };

    }
}

using FluentValidation;
using URLShorten.Models.Identities;

public class LoginVMValidator : AbstractValidator<LoginVM>
{
    public LoginVMValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}   
using System;
using DevStore.Core.Messages;
using FluentValidation;

namespace DevStore.Customers.API.Application.Commands
{
    public class NewCustomerCommand : Command
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string SocialNumber { get; set; }

        public NewCustomerCommand()
        {
            
        }

        public NewCustomerCommand(Guid id, string name, string email, string socialNumber)
        {
            AggregateId = id;
            Id = id;
            Name = name;
            Email = email;
            SocialNumber = socialNumber;
        }

        public override bool IsValid()
        {
            ValidationResult = new NewCustomerValidation().Validate(this);
            return ValidationResult.IsValid;
        }

        public class NewCustomerValidation : AbstractValidator<NewCustomerCommand>
        {
            public NewCustomerValidation()
            {
                RuleFor(c => c.Id)
                    .NotEqual(Guid.Empty)
                    .WithMessage("Invalid customer id");

                RuleFor(c => c.Name)
                    .NotEmpty()
                    .WithMessage("Customer name must be set");

                RuleFor(c => c.SocialNumber)
                    .Must(IsValidSsn)
                    .WithMessage("Invalid SSN.");

                RuleFor(c => c.Email)
                    .Must(HasValidEmail)
                    .WithMessage("Wrong e-mail.");
            }

            protected static bool IsValidSsn(string ssn)
            {
                return !string.IsNullOrEmpty(ssn);
            }

            protected static bool HasValidEmail(string email)
            {
                return Core.DomainObjects.Email.Validate(email);
            }
        }
    }
}
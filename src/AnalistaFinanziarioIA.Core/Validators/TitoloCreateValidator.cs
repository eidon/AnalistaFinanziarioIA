using FluentValidation;
using AnalistaFinanziarioIA.Core.DTOs;

namespace AnalistaFinanziarioIA.Core.Validators
{
    public class TitoloCreateValidator : AbstractValidator<TitoloCreateDto>
    {
        public TitoloCreateValidator()
        {
            RuleFor(x => x.Simbolo)
                        .NotEmpty().WithMessage("Il Simbolo è obbligatorio.")
                        .MinimumLength(2).WithMessage("Il Simbolo deve avere almeno 2 caratteri.")
                        .MaximumLength(10).WithMessage("Il Simbolo è troppo lungo.");

            RuleFor(x => x.Isin)
                .NotEmpty().WithMessage("L'ISIN è obbligatorio.")
                .Length(12).WithMessage("L'ISIN deve essere di esattamente 12 caratteri.")
                .Matches(@"^[A-Z]{2}[A-Z0-9]{9}[0-9]{1}$").WithMessage("Formato ISIN non valido (es: IT0005845678).");

            RuleFor(x => x.Valuta)
                .NotEmpty()
                .Length(3).WithMessage("La valuta deve essere di 3 lettere (es: EUR, USD).");
        }
    }
}

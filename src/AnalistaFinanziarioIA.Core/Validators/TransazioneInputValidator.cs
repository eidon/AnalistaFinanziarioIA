using FluentValidation;
using AnalistaFinanziarioIA.Core.DTOs;

namespace AnalistaFinanziarioIA.Core.Validators;

public class TransazioneInputValidator : AbstractValidator<TransazioneInputDto>
{
    public TransazioneInputValidator()
    {
        RuleFor(x => x.Quantita)
            .GreaterThan(0).WithMessage("La quantità deve essere maggiore di zero.");

        RuleFor(x => x.PrezzoUnita)
            .GreaterThan(0).WithMessage("Il prezzo per unità deve essere positivo.");

        RuleFor(x => x.Commissioni)
            .GreaterThanOrEqualTo(0).WithMessage("Le commissioni non possono essere negative.");

        RuleFor(x => x.Tasse)
            .GreaterThanOrEqualTo(0).WithMessage("Le tasse non possono essere negative.");

        RuleFor(x => x.Data)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Non puoi inserire una data futura.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Le note sono troppo lunghe (max 500 caratteri).");
    }
}
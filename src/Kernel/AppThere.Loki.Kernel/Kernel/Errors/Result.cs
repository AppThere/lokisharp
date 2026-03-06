// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Lightweight discriminated union for operations that can succeed or
//          fail with a typed error. Used in async pipelines (font download,
//          PDF export) where the error type matters to the caller.
//          Do NOT use where TryGet or typed exceptions are sufficient.
//          See ADR-004 §Tier 3 for when to use Result<T,E>.
// DEPENDS: (none)
// USED BY: IFontDownloadProvider, PDF/EPUB export (Phase 7+)
// PHASE:   1
// ADR:     ADR-004

namespace AppThere.Loki.Kernel.Errors;

public readonly struct Result<T, E>
{
    private readonly T  _value;
    private readonly E  _error;

    public bool IsOk    { get; }
    public bool IsError => !IsOk;

    public T Value
    {
        get
        {
            if (!IsOk) throw new InvalidOperationException("Result is in error state.");
            return _value;
        }
    }

    public E Error
    {
        get
        {
            if (IsOk) throw new InvalidOperationException("Result is in success state.");
            return _error;
        }
    }

    private Result(bool isOk, T value, E error)
    {
        IsOk   = isOk;
        _value = value;
        _error = error;
    }

    public static Result<T, E> Ok(T value)  => new(true,  value,   default!);
    public static Result<T, E> Fail(E error) => new(false, default!, error);

    public Result<U, E> Map<U>(Func<T, U> f) =>
        IsOk ? Result<U, E>.Ok(f(Value)) : Result<U, E>.Fail(Error);

    public Result<T, F> MapError<F>(Func<E, F> f) =>
        IsOk ? Result<T, F>.Ok(Value) : Result<T, F>.Fail(f(Error));

    public T ValueOrDefault(T fallback) => IsOk ? Value : fallback;

    public override string ToString() =>
        IsOk ? $"Ok({Value})" : $"Fail({Error})";
}

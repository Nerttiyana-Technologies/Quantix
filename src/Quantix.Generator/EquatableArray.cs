// An immutable array with value equality, for fields of the generator's cached models (plan L2-A8).

using System.Collections.Immutable;

namespace Quantix.Generator;

/// <summary>
/// An immutable array with element-wise value equality, suitable for fields of the equatable
/// models the incremental generator caches. <see cref="ImmutableArray{T}"/> itself compares by
/// reference, which would defeat the incremental cache.
/// </summary>
/// <typeparam name="T">The equatable element type.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    /// <summary>Creates an equatable array wrapping the given immutable array.</summary>
    /// <param name="array">The immutable array to wrap.</param>
    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array;
    }

    /// <summary>An empty equatable array.</summary>
    public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

    /// <summary>The number of elements in the array.</summary>
    public int Count => _array.IsDefaultOrEmpty ? 0 : _array.Length;

    /// <summary>The element at the given zero-based index.</summary>
    /// <param name="index">The zero-based index.</param>
    public T this[int index] => _array[index];

    /// <summary>Returns the underlying immutable array, never the default value.</summary>
    public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

    /// <summary>Returns an enumerator over the elements, so the array supports <c>foreach</c>.</summary>
    public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

    /// <summary>Compares this array with another for element-wise equality.</summary>
    /// <param name="other">The array to compare against.</param>
    /// <returns>True when both arrays hold equal elements in the same order.</returns>
    public bool Equals(EquatableArray<T> other)
    {
        ImmutableArray<T> left = AsImmutableArray();
        ImmutableArray<T> right = other.AsImmutableArray();

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 17;

        foreach (T item in AsImmutableArray())
        {
            hash = (hash * 31) + EqualityComparer<T>.Default.GetHashCode(item);
        }

        return hash;
    }

    /// <summary>Compares two equatable arrays for element-wise equality.</summary>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>Compares two equatable arrays for inequality.</summary>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

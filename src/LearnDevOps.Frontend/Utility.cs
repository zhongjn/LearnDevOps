namespace LearnDevOps.Frontend;

static class Utility {
    public static T NotNull<T>(this T? t) {
        return t ?? throw new ArgumentException("value is null");
    }
}

class AssertionException : Exception {
    public AssertionException(string? message = null) : base(message) {}
}
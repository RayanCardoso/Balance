using System.Collections;

namespace CommonTestUtilities.Culture;

public class CultureInlineDataTest : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return ["en"];
        yield return ["pt-BR"];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

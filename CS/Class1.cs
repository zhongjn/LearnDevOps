namespace CS;

using Nest;

public class Person
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class Class1
{
    private ElasticClient client;

    void Query()
    {
        var searchResponse = client.Search<Person>(s => s
            .From(0)
            .Size(10)
            .Query(q => q
                .Match(m => m
                   .Field(f => f.FirstName)
                   .Query("Martijn")
                )
    )
);
    }

    void Put()
    {

    }

}

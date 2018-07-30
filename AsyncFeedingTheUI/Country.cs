
using System.Windows.Data;

public class Countries
{
    public Country[] ListOfCountries { get; set; }
}

public class Country : Prism.Mvvm.BindableBase
{
    public string Code { get; set; }
    public string Name { get; set; }

    private Foo foo;

    public Foo Foo
    {
        get => foo;
        set => SetProperty(ref foo, value);
    }
}

public class Foo : Prism.Mvvm.BindableBase
{ 
    private string name;

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }
}


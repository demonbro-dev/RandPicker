using System.Collections.Generic;

public class NameList
{
    public string name { get; set; }
    public List<string> members { get; set; }
}

public class RootObject
{
    public List<NameList> name_lists { get; set; }
}
namespace LocoCalc.Models;

public class StoredConsistEntry
{
    public string          DefinitionId  { get; set; } = string.Empty;
    public string          Designation   { get; set; } = string.Empty;
    public ConsistPosition Position      { get; set; }
    public bool            BrakesEnabled { get; set; }
    public bool            IsTransported { get; set; }
    public bool            EdbActive     { get; set; }
    public bool            RModeActive   { get; set; }
    public string?         CustomName    { get; set; }
}

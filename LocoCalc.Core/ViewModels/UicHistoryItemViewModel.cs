using LocoCalc.Services;

namespace LocoCalc.ViewModels;

public class UicHistoryItemViewModel
{
    public string DefinitionId { get; }
    public string Designation  { get; }
    public string RawDigits    { get; }
    public string FormattedUic { get; }

    public UicHistoryItemViewModel(string definitionId, string designation, string rawDigits, string uicFormat)
    {
        DefinitionId = definitionId;
        Designation  = designation;
        RawDigits    = rawDigits;
        FormattedUic = UicFormatter.Format(rawDigits, uicFormat);
    }
}

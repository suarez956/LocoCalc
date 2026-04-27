using LocoCalc.Services;

namespace LocoCalc.ViewModels;

public class TractionShareViewModel
{
    public string Designation      { get; }
    public int    Twr              { get; }
    public string TwrLabel         { get; }   // e.g. "PČ 16" / "TWR 16"
    public string AssignedWeightTx { get; }   // e.g. "1320,0 t"

    public TractionShareViewModel(string designation, int twr, double weightTonnes)
    {
        Designation      = designation;
        Twr              = twr;
        TwrLabel         = LocalizationService.Instance.TwrRowFormat(twr);
        AssignedWeightTx = $"{weightTonnes:F1} t";
    }
}

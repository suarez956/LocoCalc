namespace LocoCalcAvalonia.ViewModels;

public class TractionShareViewModel
{
    public string Designation      { get; }
    public int    PomerCislo       { get; }
    public string AssignedWeightTx { get; }   // e.g. "1320,0 t"

    public TractionShareViewModel(string designation, int pc, double weightTonnes)
    {
        Designation      = designation;
        PomerCislo       = pc;
        AssignedWeightTx = $"{weightTonnes:F1} t";
    }
}

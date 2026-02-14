namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    /// <summary>
    /// Common interface for all Value Information Field types (VIF, VIFE, VIFE_FB, VIFE_FD).
    /// </summary>
    public interface IValueInformationField
    {
        bool Extension { get; }

        VariableDataQuantityUnit Units { get; }

        string? Unit { get; }

        string? Quantity { get; }

        int Magnitude { get; }

        byte Data { get; }

        string? VIF_string => null;
    }
}

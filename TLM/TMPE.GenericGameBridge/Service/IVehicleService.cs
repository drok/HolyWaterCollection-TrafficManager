namespace GenericGameBridge.Service {
    public delegate bool ParkedVehicleHandler(ushort parkedVehicleId,
                                              ref VehicleParked parkedVehicle);

    public interface IVehicleService {
        int MaxVehicleCount { get; }

        bool CheckVehicleFlags(ushort vehicleId,
                               Vehicle.Flags flagMask,
                               Vehicle.Flags? expectedResult = default(Vehicle.Flags?));

        bool CheckVehicleFlags2(ushort vehicleId,
                                Vehicle.Flags2 flagMask,
                                Vehicle.Flags2? expectedResult = default(Vehicle.Flags2?));

        bool IsVehicleValid(ushort vehicleId);

        void ProcessParkedVehicle(ushort parkedVehicleId, ParkedVehicleHandler handler);
    }
}
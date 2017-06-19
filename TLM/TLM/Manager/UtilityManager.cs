﻿using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrafficManager.State;
using TrafficManager.Geometry;
using CSUtil.Commons;
using TrafficManager.Custom.AI;
using System.Threading;
using UnityEngine;

namespace TrafficManager.Manager {
	public class UtilityManager : AbstractCustomManager {
		public static UtilityManager Instance { get; private set; } = null;

		static UtilityManager() {
			Instance = new UtilityManager();
		}

		/// <summary>
		/// Determines if stuck entities should be cleared
		/// </summary>
		private static bool ResetStuckEntitiesRequested = false;

		/// <summary>
		/// Determines if vehicles should be cleared
		/// </summary>
		private static bool ClearTrafficRequested = false;

		/// <summary>
		/// Determines if debug output should be printed
		/// </summary>
		private static bool PrintDebugInfoRequested = false;

		public void RequestResetStuckEntities() {
			ResetStuckEntitiesRequested = true;
		}

		public void RequestPrintDebugInfo() {
			PrintDebugInfoRequested = true;
		}

		internal void SimulationStep() {
			if (ResetStuckEntitiesRequested) {
				try {
					ResetStuckEntities();
				} catch (Exception e) {
					Log.Error($"Error occurred while resetting stuck entities: {e}");
				} finally {
					ResetStuckEntitiesRequested = false;
				}
			}

			if (ClearTrafficRequested) {
				try {
					ClearTraffic();
				} finally {
					ClearTrafficRequested = false;
				}
			}
#if DEBUG
			if (PrintDebugInfoRequested) {
				try {
					PrintAllDebugInfo();
				} catch (Exception e) {
					Log.Error($"Error occurred while printing debug info: {e}");
				} finally {
					PrintDebugInfoRequested = false;
				}
			}
#endif
		}

		private void ClearTraffic() {
			try {
				Monitor.Enter(Singleton<VehicleManager>.instance);

				for (ushort i = 0; i < Singleton<VehicleManager>.instance.m_vehicles.m_size; ++i) {
					if (
						(Singleton<VehicleManager>.instance.m_vehicles.m_buffer[i].m_flags & Vehicle.Flags.Created) != 0)
						Singleton<VehicleManager>.instance.ReleaseVehicle(i);
				}

				TrafficMeasurementManager.Instance.ResetTrafficStats();
			} catch (Exception ex) {
				Log.Error($"Error occured while trying to clear traffic: {ex.ToString()}");
			} finally {
				Monitor.Exit(Singleton<VehicleManager>.instance);
			}
		}

		internal void RequestClearTraffic() {
			ClearTrafficRequested = true;
		}

		private void PrintAllDebugInfo() {
			Log._Debug($"UtilityManager.PrintAllDebugInfo(): Pausing simulation.");
			Singleton<SimulationManager>.instance.ForcedSimulationPaused = true;

			Log._Debug("=== Flags.PrintDebugInfo() ===");
			try {
				Flags.PrintDebugInfo();
			} catch (Exception e) {
				Log.Error($"Error occurred while printing debug info for flags: {e}");
			}

			Log._Debug("=== SegmentGeometry.PrintDebugInfo() ===");
			try {
				SegmentGeometry.PrintDebugInfo();
			} catch (Exception e) {
				Log.Error($"Error occurred while printing debug info for segment geometries: {e}");
			}

			Log._Debug("=== NodeGeometry.PrintDebugInfo() ===");
			try {
				NodeGeometry.PrintDebugInfo();
			} catch (Exception e) {
				Log.Error($"Error occurred while printing debug info for node geometries: {e}");
			}

			foreach (ICustomManager manager in LoadingExtension.RegisteredManagers) {
				try {
					manager.PrintDebugInfo();
				} catch (Exception e) {
					Log.Error($"Error occurred while printing debug info for manager {manager.GetType().Name}: {e}");
				}
			}

			Log._Debug($"UtilityManager.PrintAllDebugInfo(): Unpausing simulation.");
			Singleton<SimulationManager>.instance.ForcedSimulationPaused = false;
		}

		private void ResetStuckEntities() {
			Log.Info($"UtilityManager.RemoveStuckEntities() called.");

			Log.Info($"UtilityManager.RemoveStuckEntities(): Pausing simulation.");
			Singleton<SimulationManager>.instance.ForcedSimulationPaused = true;

			Log.Info($"UtilityManager.RemoveStuckEntities(): Waiting for all paths.");
			Singleton<PathManager>.instance.WaitForAllPaths();

			Log.Info($"UtilityManager.RemoveStuckEntities(): Resetting citizen instances that are waiting for a path.");
			for (uint citizenInstanceId = 1; citizenInstanceId < CitizenManager.MAX_INSTANCE_COUNT; ++citizenInstanceId) {
				//Log._Debug($"UtilityManager.RemoveStuckEntities(): Processing instance {citizenInstanceId}.");
				if ((Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_flags & CitizenInstance.Flags.WaitingPath) != CitizenInstance.Flags.None) {
					CitizenAI ai = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].Info.m_citizenAI;

					if (Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_path != 0u) {
#if DEBUG
						if (GlobalConfig.Instance.DebugSwitches[3]) {
							Log._Debug($"Would reset citizen instance {citizenInstanceId} (waiting for path)");
						} else {
#endif
							Singleton<PathManager>.instance.ReleasePath(Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_path);
							Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_path = 0u;
#if DEBUG
						}
#endif
					}
					Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_flags &= ~(CitizenInstance.Flags.WaitingTransport | CitizenInstance.Flags.EnteringVehicle | CitizenInstance.Flags.BoredOfWaiting | CitizenInstance.Flags.WaitingTaxi | CitizenInstance.Flags.WaitingPath);
				} else {
#if DEBUG
					if (Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_path == 0 &&
						(Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_flags & CitizenInstance.Flags.Character) != CitizenInstance.Flags.None) {
						Log._Debug($"Found potential floating citizen instance: {citizenInstanceId} Source building: {Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_sourceBuilding} Target building: {Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_targetBuilding} Distance to target position: {(Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].GetLastFramePosition() - (Vector3)Singleton<CitizenManager>.instance.m_instances.m_buffer[citizenInstanceId].m_targetPos).magnitude}");
					}
#endif
				}
			}

			Log.Info($"UtilityManager.RemoveStuckEntities(): Resetting vehicles that are waiting for a path.");
			for (uint vehicleId = 1; vehicleId < VehicleManager.MAX_VEHICLE_COUNT; ++vehicleId) {
				//Log._Debug($"UtilityManager.RemoveStuckEntities(): Processing vehicle {vehicleId}.");
				if ((Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].m_flags & Vehicle.Flags.WaitingPath) != 0) {
					if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].m_path != 0u) {
#if DEBUG
						if (GlobalConfig.Instance.DebugSwitches[3]) {
							Log._Debug($"Would reset vehicle {vehicleId} (waiting for path)");
						} else {
#endif
							Singleton<PathManager>.instance.ReleasePath(Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].m_path);
							Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].m_path = 0u;
#if DEBUG
						}
#endif
					}
					Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].m_flags &= ~Vehicle.Flags.WaitingPath;
				}
			}

			Log.Info($"UtilityManager.RemoveStuckEntities(): Resetting vehicles that are parking and where no parked vehicle is assigned to the driver.");
			for (uint vehicleId = 1; vehicleId < VehicleManager.MAX_VEHICLE_COUNT; ++vehicleId) {
				//Log._Debug($"UtilityManager.RemoveStuckEntities(): Processing vehicle {vehicleId}.");
				if ((Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].m_flags & Vehicle.Flags.Parking) != 0) {
					ushort driverInstanceId = CustomPassengerCarAI.GetDriverInstance((ushort)vehicleId, ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId]);
					uint citizen = Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)driverInstanceId].m_citizen;
					if (citizen != 0u && Singleton<CitizenManager>.instance.m_citizens.m_buffer[(int)((UIntPtr)citizen)].m_parkedVehicle == 0) {
#if DEBUG
						if (GlobalConfig.Instance.DebugSwitches[3]) {
							Log._Debug($"Would reset vehicle {vehicleId} (parking without parked vehicle)");
						} else {
#endif
							Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleId].m_flags &= ~Vehicle.Flags.Parking;
#if DEBUG
						}
#endif
					}
				}
			}

			Log.Info($"UtilityManager.RemoveStuckEntities(): Unpausing simulation.");
			Singleton<SimulationManager>.instance.ForcedSimulationPaused = false;
		}
	}
}

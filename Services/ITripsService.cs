using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<ClientTripDTO>> GetClientTrips(int clientId);
    Task<int> AddClient(ClientDTO client);
    Task<bool> RegisterClientForTrip(int clientId, int tripId);
    Task<bool> DeleteClientFromTrip(int clientId, int tripId);

    
}
import api from "./api";

export default {
  getAll() {
    return api.get("/RoomRegistration");
  },

  approve(id, roomId) {
    return api.put(`/RoomRegistration/${id}/approve?roomId=${roomId}`);
  },

  reject(id) {
    return api.put(`/RoomRegistration/${id}/reject`);
  }
};
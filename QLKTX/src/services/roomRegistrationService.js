import api from "./api";

export default {
  getAll() {
    return api.get("/registrations");
  },

  approve(id, roomId) {
    const query = roomId ? `?roomId=${roomId}` : "";
    return api.put(`/registrations/${id}/approve${query}`);
  },

  reject(id) {
    return api.put(`/registrations/${id}/reject`);
  }
};

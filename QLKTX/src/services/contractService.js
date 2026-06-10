import api from "./api";

export default {
  getAll() {
    return api.get("/Contract");
  },

  getById(id) {
    return api.get(`/Contract/${id}`);
  },

  create(data) {
    return api.post("/Contract", data);
  }
};
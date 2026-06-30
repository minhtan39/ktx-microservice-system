import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "/api";

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        "Content-Type": "application/json"
    }
});

const auditClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        "Content-Type": "application/json"
    }
});

api.interceptors.request.use(config => {
    const token = localStorage.getItem("user_token");

    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
});

api.interceptors.response.use(
    response => {
        queueAuditLog(response.config, {
            ok: true,
            statusCode: response.status
        });
        return response;
    },
    error => {
        queueAuditLog(error.config, {
            ok: false,
            statusCode: error.response?.status || 0,
            errorMessage: error.response?.data?.message || error.message
        });
        return Promise.reject(error);
    }
);

export const getApiErrorMessage = (error, fallback = "Không xử lý được yêu cầu.") => {
    const status = error?.response?.status;
    const data = error?.response?.data;
    const isBlob = typeof Blob !== "undefined" && data instanceof Blob;
    const serverMessage = !isBlob && (
        typeof data === "string"
            ? data
            : data?.message || data?.detail || data?.title
    );

    if (serverMessage)
        return serverMessage;

    if (!error?.response) {
        return "Không kết nối được API Gateway. Kiểm tra backend/Gateway đang chạy và frontend đang trỏ đúng /api.";
    }

    if (status === 401)
        return "Phiên đăng nhập đã hết hạn hoặc token không hợp lệ. Vui lòng đăng nhập lại.";

    if (status === 403)
        return "Tài khoản hiện tại chưa có quyền thực hiện thao tác này.";

    if (status === 404)
        return "Không tìm thấy dữ liệu hoặc Gateway chưa map đúng endpoint này.";

    if (status === 502)
        return "Gateway không gọi được service phía sau. Kiểm tra ContractStudentService/RoomService/BillingService đang chạy.";

    if (status === 504)
        return "Service phía sau phản hồi quá lâu. Kiểm tra log container và kết nối giữa các service.";

    if (status >= 500)
        return "Service đang lỗi máy chủ. Kiểm tra log backend để biết nguyên nhân chi tiết.";

    return fallback;
};

export const recordAuditLog = (payload) => {
    const token = localStorage.getItem("user_token");
    if (!token) return Promise.resolve();

    return auditClient.post("/system/logs", payload, {
        headers: {
            Authorization: `Bearer ${token}`,
            "X-Skip-Audit": "true"
        }
    });
};

const queueAuditLog = (config, result) => {
    if (!shouldAudit(config)) return;

    const payload = buildAuditPayload(config, result);
    window.setTimeout(() => {
        recordAuditLog(payload).catch(() => {});
    }, 0);
};

const shouldAudit = (config) => {
    if (!config) return false;

    const method = String(config.method || "get").toLowerCase();
    if (!["post", "put", "patch", "delete"].includes(method)) return false;
    if (config.headers?.["X-Skip-Audit"]) return false;
    if (!localStorage.getItem("user_token")) return false;

    const path = normalizePath(config.url);
    if (!path || path.startsWith("/system/logs")) return false;
    if (path.includes("/read") && path.startsWith("/notifications/")) return false;
    if (path.includes("/analyze") && path.startsWith("/incidents")) return false;

    return true;
};

const normalizePath = (url = "") => {
    try {
        const parsed = new URL(url, window.location.origin);
        let path = parsed.pathname || "";
        if (path.startsWith("/api/")) path = path.slice(4);
        return path.startsWith("/") ? path : `/${path}`;
    } catch {
        const [path] = String(url).split("?");
        return path.startsWith("/") ? path : `/${path}`;
    }
};

const buildAuditPayload = (config, result) => {
    const path = normalizePath(config.url);
    const method = String(config.method || "get").toUpperCase();
    const descriptor = describeAuditAction(path, method);

    return {
        module: descriptor.module,
        action: descriptor.action,
        status: result.ok ? "Success" : "Failed",
        targetType: descriptor.targetType,
        targetId: descriptor.targetId,
        targetName: descriptor.targetName,
        description: result.ok
            ? `${descriptor.action} thành công.`
            : `${descriptor.action} thất bại${result.statusCode ? ` với mã ${result.statusCode}` : ""}.`,
        metadata: {
            method,
            path,
            statusCode: result.statusCode,
            errorMessage: result.errorMessage || "",
            actor: localStorage.getItem("username") || localStorage.getItem("fullName") || ""
        }
    };
};

const describeAuditAction = (path, method) => {
    const segments = path.split("/").filter(Boolean);
    const first = segments[0] || "";
    const second = segments[1] || "";
    const last = segments[segments.length - 1] || "";
    const id = segments.find(item => /^\d+$/.test(item)) || second || "";

    if (first === "auth") {
        if (second === "login") return makeAudit("AuthService", "Đăng nhập", "Account", "");
        if (second === "change-password") return makeAudit("AuthService", "Đổi mật khẩu", "Account", "");
        if (second === "forgot-password") return makeAudit("AuthService", "Yêu cầu quên mật khẩu", "Account", "");
        if (second === "reset-password") return makeAudit("AuthService", "Đặt lại mật khẩu", "Account", "");
        if (second === "accounts") {
            if (last === "access-link") return makeAudit("Quản lý tài khoản", "Gửi link kích hoạt tài khoản", "Account", id);
            return makeAudit("Quản lý tài khoản", method === "POST" ? "Tạo tài khoản" : "Cập nhật tài khoản", "Account", id);
        }
        if (second === "student-accounts") return makeAudit("Quản lý tài khoản", "Tạo tài khoản sinh viên", "Account", "");
        return makeAudit("AuthService", "Thao tác tài khoản", "Account", id);
    }

    if (["students", "registrations", "contracts"].includes(first)) {
        const module = first === "students" ? "Hồ sơ sinh viên" : first === "registrations" ? "Đăng ký nội trú" : "Hợp đồng";
        const action = method === "POST"
            ? `Tạo ${module.toLowerCase()}`
            : method === "DELETE"
                ? `Xóa ${module.toLowerCase()}`
                : `Cập nhật ${module.toLowerCase()}`;
        return makeAudit(module, action, first, id);
    }

    if (["rooms", "buildings", "roomtypes", "room-types"].includes(first)) {
        return makeAudit("Room & Building", `${methodLabel(method)} dữ liệu phòng/tòa`, first, id);
    }

    if (first === "billing") {
        if (path.includes("mark-paid")) return makeAudit("Hóa đơn & thanh toán", "Xác nhận thanh toán hóa đơn", "Invoice", id);
        if (path.includes("resend-email")) return makeAudit("Hóa đơn & thanh toán", "Gửi lại email hóa đơn", "Invoice", id);
        if (path.includes("room/issue")) return makeAudit("Hóa đơn & thanh toán", "Phát hành phiếu theo phòng", "RoomInvoiceBatch", "");
        if (path.includes("wallets")) return makeAudit("Ví sinh viên", "Cập nhật ví sinh viên", "StudentWallet", id);
        return makeAudit("Hóa đơn & thanh toán", `${methodLabel(method)} dữ liệu hóa đơn`, "Billing", id);
    }

    if (first === "incidents") {
        if (last === "assign") return makeAudit("Sửa chữa & bảo trì", "Phân công yêu cầu sửa chữa", "Incident", id);
        if (last === "status") return makeAudit("Sửa chữa & bảo trì", "Cập nhật tiến độ sửa chữa", "Incident", id);
        if (last === "confirm") return makeAudit("Sửa chữa & bảo trì", "Sinh viên xác nhận sửa chữa", "Incident", id);
        if (last === "reopen") return makeAudit("Sửa chữa & bảo trì", "Yêu cầu xử lý lại", "Incident", id);
        if (last === "cancel") return makeAudit("Sửa chữa & bảo trì", "Hủy yêu cầu sửa chữa", "Incident", id);
        return makeAudit("Sửa chữa & bảo trì", method === "POST" ? "Gửi yêu cầu sửa chữa" : "Cập nhật yêu cầu sửa chữa", "Incident", id);
    }

    if (first === "maintenance") {
        return makeAudit("Sửa chữa & bảo trì", method === "POST" ? "Tạo lịch bảo trì" : "Cập nhật lịch bảo trì", "MaintenancePlan", id);
    }

    if (first === "notifications") {
        if (last === "status") return makeAudit("Thông báo hệ thống", "Cập nhật trạng thái thông báo", "Notification", id);
        return makeAudit("Thông báo hệ thống", "Tạo thông báo hệ thống", "Notification", id);
    }

    return makeAudit("Hệ thống", `${methodLabel(method)} dữ liệu`, first || "System", id);
};

const makeAudit = (module, action, targetType, targetId) => ({
    module,
    action,
    targetType,
    targetId,
    targetName: targetId ? `${targetType} #${targetId}` : targetType
});

const methodLabel = (method) => ({
    POST: "Tạo",
    PUT: "Cập nhật",
    PATCH: "Cập nhật",
    DELETE: "Xóa"
}[method] || "Thao tác");

export default api;

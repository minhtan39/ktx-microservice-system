<template>
  <section class="student-portal">
    <div class="student-hero">
      <div>
        <span class="page-kicker">Student self-service</span>
        <h2>Xin chào, {{ displayName }}</h2>
        <p>Theo dõi hồ sơ, gửi đăng ký nội trú và xem hợp đồng ký túc xá của bạn.</p>
      </div>

      <div class="hero-stats">
        <div class="stat-tile">
          <span class="mdi mdi-clipboard-text-outline"></span>
          <strong>{{ ownRegistrations.length }}</strong>
          <small>Đơn đăng ký</small>
        </div>
        <div class="stat-tile">
          <span class="mdi mdi-file-document-check-outline"></span>
          <strong>{{ ownContracts.length }}</strong>
          <small>Hợp đồng</small>
        </div>
        <div class="stat-tile">
          <span class="mdi mdi-bed-outline"></span>
          <strong>{{ currentRoom }}</strong>
          <small>Phòng hiện tại</small>
        </div>
      </div>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
      {{ error }}
    </v-alert>

    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">
      {{ success }}
    </v-alert>

    <div v-if="loading" class="loading-state">
      <v-progress-circular indeterminate color="success" />
      <span>Đang tải dữ liệu sinh viên...</span>
    </div>

    <template v-else>
      <div class="portal-grid">
        <section class="panel profile-panel">
          <div class="panel-title">
            <span class="mdi mdi-account-school-outline"></span>
            <div>
              <strong>Hồ sơ sinh viên</strong>
              <small>{{ student?.studentCode || studentCode || 'Chưa có mã sinh viên' }}</small>
            </div>
          </div>

          <div v-if="student" class="profile-list">
            <div>
              <span>Họ tên</span>
              <strong>{{ student.fullName }}</strong>
            </div>
            <div>
              <span>Khoa</span>
              <strong>{{ student.facultyName || 'Chưa cập nhật' }}</strong>
            </div>
            <div>
              <span>Lớp</span>
              <strong>{{ student.className || 'Chưa cập nhật' }}</strong>
            </div>
            <div>
              <span>Liên hệ</span>
              <strong>{{ student.phone || 'Chưa cập nhật' }}</strong>
            </div>
            <div>
              <span>Trạng thái</span>
              <strong>{{ statusLabel(student.status) }}</strong>
            </div>
            <div>
              <span>Lịch sử lưu trú</span>
              <strong>{{ student.residenceHistory || 'Chưa có lịch sử lưu trú' }}</strong>
            </div>
          </div>

          <div v-else class="empty-state">
            Chưa tìm thấy hồ sơ sinh viên tương ứng với tài khoản đăng nhập.
          </div>
        </section>

        <section class="panel registration-panel">
          <div class="panel-title">
            <span class="mdi mdi-form-select"></span>
            <div>
              <strong>Đăng ký nội trú online</strong>
              <small>Đơn sẽ chuyển sang màn duyệt của admin/nhân viên</small>
            </div>
          </div>

          <v-form class="registration-form" @submit.prevent="submitRegistration">
            <v-alert
              v-if="registrationBlockReason"
              type="warning"
              variant="tonal"
              class="mb-4"
            >
              {{ registrationBlockReason }}
            </v-alert>
            <v-alert v-if="registrationError" type="error" variant="tonal" closable class="mb-4" @click:close="registrationError = ''">
              {{ registrationError }}
            </v-alert>
            <v-alert v-if="registrationSuccess" type="success" variant="tonal" closable class="mb-4" @click:close="registrationSuccess = ''">
              {{ registrationSuccess }}
            </v-alert>

            <v-row dense>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="form.buildingName"
                  :items="buildingOptions"
                  label="Tòa mong muốn"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="form.roomType"
                  :items="roomTypeOptions"
                  label="Loại phòng"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="form.priorityType"
                  :items="priorityOptions"
                  item-title="label"
                  item-value="value"
                  label="Diện ưu tiên"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.priorityNote"
                  label="Ghi chú ưu tiên"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.startDate"
                  type="date"
                  label="Ngày bắt đầu"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.endDate"
                  type="date"
                  label="Ngày kết thúc"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <div class="form-actions">
              <v-btn
                color="success"
                type="submit"
                :loading="submitting"
                :disabled="!canSubmitRegistration"
                prepend-icon="mdi-send-outline"
              >
                Gửi đăng ký
              </v-btn>
              <v-btn
                variant="tonal"
                color="success"
                prepend-icon="mdi-refresh"
                @click="loadAll"
              >
                Làm mới
              </v-btn>
            </div>
          </v-form>
        </section>
      </div>

      <section class="panel">
        <div class="panel-title">
          <span class="mdi mdi-tools"></span>
          <div>
            <strong>Yêu cầu sửa chữa</strong>
            <small>Sinh viên gửi sự cố phòng ở, nhân viên/admin tiếp nhận và cập nhật trạng thái</small>
          </div>
        </div>

        <v-alert v-if="incidentError" type="error" variant="tonal" closable class="mb-4" @click:close="incidentError = ''">
          {{ incidentError }}
        </v-alert>
        <v-alert v-if="incidentSuccess" type="success" variant="tonal" closable class="mb-4" @click:close="incidentSuccess = ''">
          {{ incidentSuccess }}
        </v-alert>

        <v-form class="registration-form" @submit.prevent="submitIncident">
          <v-row dense>
            <v-col cols="12" sm="6">
              <div :class="['auto-room-field', { 'is-missing': !repairRoom }]">
                <span class="mdi mdi-home-city-outline"></span>
                <div>
                  <small>Phòng báo sửa chữa</small>
                  <strong>{{ repairRoomLabel }}</strong>
                </div>
              </div>
            </v-col>
            <v-col cols="12" sm="3">
              <v-select
                v-model="incidentForm.category"
                :items="incidentCategories"
                item-title="title"
                item-value="value"
                label="Loại sự cố"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="3">
              <v-select
                v-model="incidentForm.priority"
                :items="incidentPriorityOptions"
                item-title="title"
                item-value="value"
                label="Mức độ ưu tiên"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="incidentForm.preferredVisitAt"
                type="datetime-local"
                label="Thời gian có thể tiếp nhận"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="incidentForm.contactPhone"
                label="Số điện thoại liên hệ"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12">
              <v-textarea
                v-model="incidentForm.description"
                label="Mô tả chi tiết"
                rows="3"
                density="comfortable"
              />
            </v-col>
            <v-col v-if="incidentAiSuggestion" cols="12">
              <div class="ai-suggestion-panel">
                <span class="mdi mdi-auto-fix"></span>
                <div>
                  <strong>{{ incidentAiSuggestion.summary }}</strong>
                  <p>{{ incidentAiSuggestion.suggestedAction }}</p>
                  <small>
                    {{ incidentCategoryLabel(incidentAiSuggestion.category) }}
                    · {{ incidentPriorityLabel(incidentAiSuggestion.priority) }}
                    · {{ incidentAiSuggestion.expectedHandlingTime }}
                    · {{ incidentAiSuggestion.source }}
                  </small>
                  <small v-if="incidentAiSuggestion.safetyNote">{{ incidentAiSuggestion.safetyNote }}</small>
                </div>
              </div>
            </v-col>
          </v-row>

          <div class="form-actions">
            <v-btn
              color="primary"
              variant="tonal"
              :loading="incidentAnalyzing"
              :disabled="!incidentForm.description.trim()"
              prepend-icon="mdi-auto-fix"
              @click="analyzeIncident"
            >
              AI phân tích
            </v-btn>
            <v-btn
              color="success"
              type="submit"
              :loading="incidentSubmitting"
              :disabled="!student || !repairRoom"
              prepend-icon="mdi-send-outline"
            >
              Gửi yêu cầu sửa chữa
            </v-btn>
          </div>
        </v-form>

        <div class="table-wrap mt-4">
          <table>
            <thead>
              <tr>
                <th>Ngày gửi</th>
                <th>Phòng</th>
                <th>Mức độ</th>
                <th>Loại sự cố</th>
                <th>Mô tả</th>
                <th>Phụ trách</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="ownIncidents.length === 0">
                <td colspan="8" class="empty-cell">Bạn chưa có yêu cầu sửa chữa nào.</td>
              </tr>
              <tr v-for="incident in ownIncidents" :key="incident.id">
                <td>{{ formatDate(incident.createdAt) }}</td>
                <td>{{ roomDisplay(incident) }}</td>
                <td><span class="status-pill" :class="`priority-${incident.priority || 'normal'}`">{{ incidentPriorityLabel(incident.priority) }}</span></td>
                <td>{{ incidentCategoryLabel(incident.category) }}</td>
                <td>{{ incident.description }}</td>
                <td>
                  <strong>{{ incident.assignedName || 'Chưa phân công' }}</strong>
                  <small class="table-note">{{ incident.dueAt ? `Hạn ${formatDateTime(incident.dueAt)}` : incident.staffNote || '-' }}</small>
                </td>
                <td>
                  <span class="status-pill" :class="incidentStatusClass(incident.status)">
                    {{ incidentStatusLabel(incident.status) }}
                  </span>
                </td>
                <td>
                  <div v-if="incident.status === 'completed'" class="incident-actions">
                    <v-btn size="small" color="success" :loading="incidentActionLoading === incident.id" @click="confirmIncident(incident)">Đã sửa đạt</v-btn>
                    <v-btn size="small" variant="tonal" color="warning" :disabled="incidentActionLoading === incident.id" @click="openReopenDialog(incident)">Chưa đạt</v-btn>
                  </div>
                  <v-btn v-else-if="incident.status === 'confirmed'" size="small" variant="text" color="warning" @click="openReopenDialog(incident)">Yêu cầu xử lý lại</v-btn>
                  <v-btn v-else-if="canCancelIncident(incident)" size="small" variant="text" color="error" :loading="incidentActionLoading === incident.id" @click="cancelIncident(incident)">Hủy yêu cầu</v-btn>
                  <span v-else class="table-note">{{ incident.staffNote || '-' }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel">
        <div class="panel-title">
          <span class="mdi mdi-clipboard-list-outline"></span>
          <div>
            <strong>Đơn đăng ký của tôi</strong>
            <small>Admin/nhân viên duyệt tại mục Duyệt xếp phòng</small>
          </div>
        </div>

        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tòa</th>
                <th>Loại phòng</th>
                <th>Ưu tiên</th>
                <th>Thời gian ở</th>
                <th>Trạng thái</th>
                <th>Phòng xếp</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="ownRegistrations.length === 0">
                <td colspan="6" class="empty-cell">Bạn chưa có đơn đăng ký nội trú.</td>
              </tr>
              <tr v-for="registration in ownRegistrations" :key="registration.id">
                <td>{{ registration.buildingName }}</td>
                <td>{{ registration.roomType }}</td>
                <td>{{ priorityLabel(registration.priorityType) }}</td>
                <td>{{ formatDate(registration.startDate) }} - {{ formatDate(registration.endDate) }}</td>
                <td>
                  <span class="status-pill" :class="statusClass(registration.status)">
                    {{ statusLabel(registration.status) }}
                  </span>
                  <small v-if="registration.rejectionReason" class="table-note">
                    {{ registration.rejectionReason }}
                  </small>
                </td>
                <td>{{ registration.assignedRoomId ? `#${registration.assignedRoomId}` : 'Chưa xếp' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel">
        <div class="panel-title">
          <span class="mdi mdi-file-document-outline"></span>
          <div>
            <strong>Hợp đồng của tôi</strong>
            <small>Hợp đồng được tạo tự động sau khi đơn được duyệt</small>
          </div>
        </div>

        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Mã hợp đồng</th>
                <th>Phòng</th>
                <th>Thời hạn</th>
                <th>Tiền cọc</th>
                <th>Tiền phòng</th>
                <th>Trạng thái</th>
                <th>Ký online</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="ownContracts.length === 0">
                <td colspan="7" class="empty-cell">Chưa có hợp đồng nào được tạo.</td>
              </tr>
              <tr v-for="contract in ownContracts" :key="contract.id">
                <td>{{ contract.contractCode }}</td>
                <td>#{{ contract.roomId }}</td>
                <td>{{ formatDate(contract.startDate) }} - {{ formatDate(contract.endDate) }}</td>
                <td>{{ money(contract.depositAmount) }}</td>
                <td>{{ money(contract.monthlyFee) }}</td>
                <td>
                  <span class="status-pill" :class="statusClass(contract.status)">
                    {{ statusLabel(contract.status) }}
                  </span>
                </td>
                <td>
                  <div class="contract-sign-cell">
                    <strong>{{ contract.signedAt ? 'Đã ký online' : 'Chưa ký' }}</strong>
                    <small class="table-note">
                      {{ contract.signedAt ? formatDateTime(contract.signedAt) : 'Cần ký để xác nhận điều khoản' }}
                    </small>
                    <small class="table-note">
                      PDF: {{ contract.templateFilePath ? 'Đã có hợp đồng để ký' : 'Chờ admin phát hành PDF' }}
                    </small>
                    <v-btn
                      v-if="canSignContract(contract)"
                      size="small"
                      color="success"
                      variant="tonal"
                      @click="openSignDialog(contract)"
                    >
                      Xem & ký
                    </v-btn>
                    <v-btn
                      v-else
                      size="small"
                      variant="text"
                      color="primary"
                      @click="openSignDialog(contract)"
                    >
                      Xem hợp đồng
                    </v-btn>
                    <v-btn
                      v-if="contract.templateFilePath"
                      size="small"
                      variant="text"
                      color="primary"
                      @click="openContractPdf(contract, 'template')"
                    >
                      Mở PDF mẫu
                    </v-btn>
                    <v-btn
                      v-if="contract.signedFilePath"
                      size="small"
                      variant="text"
                      color="success"
                      @click="openContractPdf(contract, 'signed')"
                    >
                      Mở bản đã ký
                    </v-btn>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <v-dialog v-model="signDialog" max-width="860">
        <v-card v-if="signTarget" class="contract-sign-dialog">
          <v-card-title class="dialog-title">
            <div>
              <span class="page-kicker">Online Contract</span>
              <strong>Hợp đồng nội trú {{ signTarget.contractCode }}</strong>
            </div>
            <v-btn icon="mdi-close" variant="text" @click="signDialog = false" />
          </v-card-title>
          <v-card-text>
            <v-alert v-if="contractError" type="error" variant="tonal" closable class="mb-4" @click:close="contractError = ''">
              {{ contractError }}
            </v-alert>
            <v-alert v-if="contractSuccess" type="success" variant="tonal" closable class="mb-4" @click:close="contractSuccess = ''">
              {{ contractSuccess }}
            </v-alert>
            <v-alert
              v-if="!signTarget.templateFilePath"
              type="warning"
              variant="tonal"
              class="mb-4"
            >
              Hợp đồng này chưa có file PDF do admin/nhân viên phát hành. Bạn chỉ có thể ký sau khi nhà trường tạo hoặc tải lên hợp đồng chuẩn.
            </v-alert>

            <div class="contract-file-actions">
              <v-btn
                color="primary"
                variant="tonal"
                prepend-icon="mdi-file-pdf-box"
                :disabled="!signTarget.templateFilePath"
                @click="openContractPdf(signTarget, 'template')"
              >
                Xem PDF mẫu
              </v-btn>
              <v-btn
                color="success"
                variant="tonal"
                prepend-icon="mdi-file-check-outline"
                :disabled="!signTarget.signedFilePath"
                @click="openContractPdf(signTarget, 'signed')"
              >
                Xem bản đã ký
              </v-btn>
            </div>

            <section class="contract-paper">
              <div class="contract-paper-head">
                <div>
                  <span>Ký túc xá sinh viên</span>
                  <h3>HỢP ĐỒNG THUÊ PHÒNG Ở KÝ TÚC XÁ</h3>
                </div>
                <strong>{{ signTarget.contractCode }}</strong>
              </div>

              <div class="contract-paper-grid">
                <div>
                  <span>Bên A</span>
                  <strong>Ban quản lý ký túc xá</strong>
                </div>
                <div>
                  <span>Bên B</span>
                  <strong>{{ student?.fullName || displayName }}</strong>
                  <small>MSSV: {{ student?.studentCode || studentCode }}</small>
                </div>
                <div>
                  <span>Phòng ở</span>
                  <strong>#{{ signTarget.roomId }}</strong>
                </div>
                <div>
                  <span>Thời hạn</span>
                  <strong>{{ formatDate(signTarget.startDate) }} - {{ formatDate(signTarget.endDate) }}</strong>
                </div>
                <div>
                  <span>Tiền cọc</span>
                  <strong>{{ money(signTarget.depositAmount) }}</strong>
                </div>
                <div>
                  <span>Tiền phòng tháng</span>
                  <strong>{{ money(signTarget.monthlyFee) }}</strong>
                </div>
              </div>

              <div class="contract-terms">
                <strong>Điều khoản chính</strong>
                <p>{{ signTarget.terms || 'Sinh viên sử dụng phòng đúng thời hạn, đóng tiền đúng hạn, tuân thủ nội quy ký túc xá và bàn giao phòng khi kết thúc hợp đồng.' }}</p>
              </div>

              <div v-if="signTarget.signedAt" class="signature-proof">
                <span class="mdi mdi-shield-check-outline"></span>
                <div>
                  <strong>Đã ký online lúc {{ formatDateTime(signTarget.signedAt) }}</strong>
                  <small>Người ký: {{ signTarget.signatureFullName || displayName }} - {{ signTarget.signatureStudentCode || studentCode }}</small>
                  <small v-if="signTarget.signatureHash">Mã xác thực: {{ signTarget.signatureHash }}</small>
                </div>
              </div>

              <div v-else class="signature-form">
                <v-checkbox
                  v-model="signAccepted"
                  label="Tôi đã đọc, hiểu và đồng ý với toàn bộ điều khoản hợp đồng nội trú."
                  color="success"
                />
                <v-row dense>
                  <v-col cols="12" sm="6">
                    <v-text-field v-model="signForm.fullName" label="Họ tên người ký" density="comfortable" />
                  </v-col>
                  <v-col cols="12" sm="6">
                    <v-text-field v-model="signForm.studentCode" label="Mã sinh viên" density="comfortable" />
                  </v-col>
                </v-row>
                <div class="signature-pad-block">
                  <div class="signature-pad-head">
                    <div>
                      <strong>Chữ ký tay</strong>
                      <small>Ký trực tiếp bằng chuột hoặc ngón tay. Chữ ký này sẽ được đóng vào file PDF đã ký.</small>
                    </div>
                    <v-btn size="small" variant="text" color="warning" @click="clearSignature">
                      Ký lại
                    </v-btn>
                  </div>
                  <canvas
                    ref="signatureCanvas"
                    class="signature-canvas"
                    @pointerdown="startSignature"
                    @pointermove="drawSignature"
                    @pointerup="stopSignature"
                    @pointerleave="stopSignature"
                    @pointercancel="stopSignature"
                  ></canvas>
                  <small class="table-note">
                    {{ hasSignature ? 'Đã ghi nhận nét ký.' : 'Chưa có chữ ký tay.' }}
                  </small>
                </div>
              </div>
            </section>
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn variant="text" @click="signDialog = false">Đóng</v-btn>
            <v-btn
              v-if="!signTarget.signedAt"
              color="success"
              :loading="signing"
              :disabled="!signAccepted || !hasSignature || !signTarget.templateFilePath"
              @click="submitSignContract"
            >
              Ký hợp đồng online
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <v-dialog v-model="reopenDialog" max-width="520">
        <v-card>
          <v-card-title>Yêu cầu xử lý lại</v-card-title>
          <v-card-text>
            <v-alert v-if="reopenError" type="error" variant="tonal" closable class="mb-4" @click:close="reopenError = ''">
              {{ reopenError }}
            </v-alert>
            <p class="dialog-copy">Hãy mô tả phần chưa đạt để nhân viên tiếp tục xử lý đúng vấn đề.</p>
            <v-textarea v-model="reopenNote" label="Lý do chưa đạt" rows="4" variant="outlined" autofocus />
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn variant="text" @click="reopenDialog = false">Hủy</v-btn>
            <v-btn color="warning" :loading="incidentActionLoading === reopenIncidentTarget?.id" @click="submitReopen">Gửi lại yêu cầu</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </template>
  </section>
</template>

<script setup>
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'
import { cleanStudents, normalizeList } from '../utils/studentDisplay'

const loading = ref(true)
const submitting = ref(false)
const error = ref('')
const success = ref('')
const registrationError = ref('')
const registrationSuccess = ref('')
const incidentError = ref('')
const incidentSuccess = ref('')
const contractError = ref('')
const contractSuccess = ref('')
const student = ref(null)
const ownRegistrations = ref([])
const ownContracts = ref([])
const ownIncidents = ref([])
const incidentActionLoading = ref(null)
const reopenDialog = ref(false)
const reopenIncidentTarget = ref(null)
const reopenNote = ref('')
const reopenError = ref('')
const signDialog = ref(false)
const signTarget = ref(null)
const signing = ref(false)
const signAccepted = ref(false)
const signatureCanvas = ref(null)
const hasSignature = ref(false)
let drawingSignature = false
let lastSignaturePoint = null
const signForm = reactive({
  fullName: '',
  studentCode: '',
})

const studentId = ref(Number(localStorage.getItem('student_id') || 0))
const studentCode = ref(
  localStorage.getItem('student_code') ||
  (localStorage.getItem('user_role') === 'Student'
    ? localStorage.getItem('username')
    : '') ||
  '')
const displayName = computed(() =>
  student.value?.fullName || localStorage.getItem('fullName') || 'Sinh viên')

const buildingOptions = ['A', 'B', 'C']
const roomTypeOptions = ['4-bed', '6-bed', '8-bed']
const priorityOptions = [
  { label: 'Không ưu tiên', value: '' },
  { label: 'Sinh viên ở xa', value: 'far' },
  { label: 'Hoàn cảnh khó khăn', value: 'poor' },
  { label: 'Diện chính sách', value: 'policy' },
  { label: 'Khuyết tật', value: 'disabled' },
  { label: 'Thành tích tốt', value: 'excellent' },
]

const form = reactive({
  buildingName: 'A',
  roomType: '4-bed',
  priorityType: '',
  priorityNote: '',
  startDate: '',
  endDate: '',
})

const incidentCategories = [
  { title: 'Điện', value: 'Electric' },
  { title: 'Nước', value: 'Water' },
  { title: 'Nội thất', value: 'Furniture' },
  { title: 'Internet', value: 'Internet' },
  { title: 'An toàn', value: 'Safety' },
  { title: 'Vệ sinh', value: 'Sanitation' },
  { title: 'Khác', value: 'Other' },
]
const incidentPriorityOptions = [
  { title: 'Thấp', value: 'low' },
  { title: 'Bình thường', value: 'normal' },
  { title: 'Cao', value: 'high' },
  { title: 'Khẩn cấp', value: 'urgent' },
]
const incidentSubmitting = ref(false)
const incidentAnalyzing = ref(false)
const incidentAiSuggestion = ref(null)
const incidentForm = reactive({
  category: 'Electric',
  priority: 'normal',
  preferredVisitAt: '',
  contactPhone: '',
  description: '',
})

const activeRegistration = computed(() =>
  ownRegistrations.value.find((registration) =>
    String(registration.status || '').toLowerCase() === 'pending'))

const activeHousingContract = computed(() =>
  ownContracts.value.find((contract) =>
    ['active', 'pendingsignature'].includes(String(contract.status || '').toLowerCase())))

const repairRoom = computed(() => {
  const contract = activeHousingContract.value

  if (!contract?.roomId)
    return null

  return {
    roomName: String(contract.roomId),
    building: contract.buildingName || contract.building || '',
  }
})

const repairRoomLabel = computed(() => {
  if (!repairRoom.value)
    return 'Chưa có phòng đang thuê'

  return repairRoom.value.building
    ? `${repairRoom.value.building}-${repairRoom.value.roomName}`
    : `Phòng #${repairRoom.value.roomName}`
})

const currentRoom = computed(() =>
  repairRoom.value ? repairRoomLabel.value : student.value?.residenceHistory ? 'Có' : 'Chưa có')

const registrationBlockReason = computed(() => {
  if (!student.value) return 'Chưa tìm thấy hồ sơ sinh viên nên chưa thể gửi đăng ký.'

  if (activeHousingContract.value) {
    return `Bạn đang có hợp đồng ${activeHousingContract.value.contractCode} còn hiệu lực, không thể gửi thêm đơn đăng ký nội trú.`
  }

  if (activeRegistration.value) {
    return `Bạn đã có đơn #${activeRegistration.value.id} đang chờ xử lý. Vui lòng chờ cán bộ xử lý trước khi gửi đơn mới.`
  }

  return ''
})

const canSubmitRegistration = computed(() =>
  Boolean(student.value) &&
  !registrationBlockReason.value &&
  !submitting.value)

const setDefaultDates = () => {
  const start = new Date()
  const end = new Date()
  end.setMonth(end.getMonth() + 10)

  form.startDate = toInputDate(start)
  form.endDate = toInputDate(end)
}

const toInputDate = (date) => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const toIsoDate = (value) => new Date(`${value}T00:00:00`).toISOString()

const resolveStudent = async () => {
  const response = await api.get('/students')
  const students = cleanStudents(response.data)
  const normalizedStudentCode = String(studentCode.value || '').trim().toLowerCase()

  const matchedByCode = normalizedStudentCode
    ? students.find((item) =>
      String(item.studentCode || '').trim().toLowerCase() === normalizedStudentCode)
    : null
  const matchedById = !matchedByCode && studentId.value
    ? students.find((item) => Number(item.id) === studentId.value)
    : null
  const matched = matchedByCode || matchedById

  student.value = matched || null

  if (student.value) {
    studentId.value = Number(student.value.id)
    studentCode.value = student.value.studentCode || studentCode.value
    localStorage.setItem('student_id', String(studentId.value))
    localStorage.setItem('student_code', studentCode.value)
    localStorage.setItem('fullName', student.value.fullName)
    incidentForm.contactPhone = incidentForm.contactPhone || student.value.phone || ''
  }
}

const loadRegistrations = async () => {
  const response = await api.get('/registrations')
  const all = normalizeList(response.data)

  ownRegistrations.value = all
    .filter((registration) => Number(registration.studentId) === studentId.value)
    .sort((first, second) =>
      new Date(second.createdAt || 0) - new Date(first.createdAt || 0))
}

const loadContracts = async () => {
  if (!studentId.value) {
    ownContracts.value = []
    return
  }

  try {
    const response = await api.get(`/contracts/student/${studentId.value}`)
    ownContracts.value = normalizeList(response.data)
  } catch {
    const response = await api.get('/contracts')
    ownContracts.value = normalizeList(response.data)
      .filter((contract) => Number(contract.studentId) === studentId.value)
  }
}

const loadIncidents = async () => {
  if (!studentId.value) {
    ownIncidents.value = []
    return
  }

  const response = await api.get(`/incidents?studentId=${studentId.value}`)
  ownIncidents.value = normalizeList(response.data)
    .filter((incident) => isOwnIncident(incident))
}

const isOwnIncident = (incident) => {
  const incidentStudentId = Number(incident.studentId || 0)
  const incidentStudentCode = String(incident.studentCode || '').trim().toLowerCase()
  const currentStudentCode = String(studentCode.value || '').trim().toLowerCase()

  return Boolean(studentId.value && incidentStudentId === studentId.value) ||
    Boolean(currentStudentCode && incidentStudentCode === currentStudentCode)
}

const loadAll = async () => {
  try {
    loading.value = true
    error.value = ''
    success.value = ''

    await resolveStudent()
    await Promise.all([
      loadRegistrations(),
      loadContracts(),
      loadIncidents(),
    ])
  } catch (err) {
    error.value = 'Không tải được dữ liệu sinh viên. Kiểm tra Gateway và ContractStudentService.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const analyzeIncident = async () => {
  if (!incidentForm.description.trim()) {
    incidentError.value = 'Vui lòng nhập mô tả sự cố trước khi dùng AI phân tích.'
    return
  }

  try {
    incidentAnalyzing.value = true
    incidentError.value = ''
    incidentSuccess.value = ''

    const response = await api.post('/incidents/analyze', {
      description: incidentForm.description,
      roomName: repairRoom.value?.roomName || '',
      building: repairRoom.value?.building || '',
      category: incidentForm.category,
      priority: incidentForm.priority,
    })
    const analysis = response.data?.data || response.data

    if (analysis?.category) incidentForm.category = analysis.category
    if (analysis?.priority) incidentForm.priority = analysis.priority

    incidentAiSuggestion.value = analysis
    incidentSuccess.value = 'AI đã phân tích và gợi ý mức độ xử lý.'
  } catch (err) {
    incidentError.value = err.response?.data?.message || 'Không phân tích được sự cố bằng AI.'
  } finally {
    incidentAnalyzing.value = false
  }
}

const submitIncident = async () => {
  if (!student.value) {
    incidentError.value = 'Chưa tìm thấy hồ sơ sinh viên để gửi yêu cầu sửa chữa.'
    return
  }

  if (!repairRoom.value) {
    incidentError.value = 'Bạn chưa có hợp đồng hoặc phòng đang ở để gửi yêu cầu sửa chữa.'
    return
  }

  if (!incidentForm.description.trim()) {
    incidentError.value = 'Vui lòng nhập mô tả sự cố.'
    return
  }

  try {
    incidentSubmitting.value = true
    incidentError.value = ''
    incidentSuccess.value = ''

    await api.post('/incidents', {
      studentId: studentId.value,
      studentCode: studentCode.value,
      studentName: displayName.value,
      roomName: repairRoom.value.roomName,
      building: repairRoom.value.building,
      category: incidentForm.category,
      priority: incidentForm.priority,
      preferredVisitAt: incidentForm.preferredVisitAt
        ? new Date(incidentForm.preferredVisitAt).toISOString()
        : null,
      contactPhone: incidentForm.contactPhone,
      description: incidentForm.description,
      aiSummary: incidentAiSuggestion.value?.summary || null,
      aiSuggestedAction: incidentAiSuggestion.value?.suggestedAction || null,
      aiExpectedHandlingTime: incidentAiSuggestion.value?.expectedHandlingTime || null,
      aiSafetyNote: incidentAiSuggestion.value?.safetyNote || null,
      aiSource: incidentAiSuggestion.value?.source || null,
    })

    incidentSuccess.value = 'Đã gửi yêu cầu sửa chữa. Nhân viên/admin sẽ tiếp nhận và cập nhật trạng thái.'
    incidentForm.description = ''
    incidentForm.preferredVisitAt = ''
    incidentAiSuggestion.value = null
    await loadIncidents()
  } catch (err) {
    incidentError.value = 'Không gửi được yêu cầu sửa chữa. Kiểm tra Gateway và BillingService.'
    console.error(err)
  } finally {
    incidentSubmitting.value = false
  }
}

const confirmIncident = async (incident) => {
  try {
    incidentActionLoading.value = incident.id
    incidentError.value = ''
    incidentSuccess.value = ''
    await api.post(`/incidents/${incident.id}/confirm`, {
      note: 'Sinh viên xác nhận yêu cầu đã được xử lý đạt yêu cầu.',
      studentId: studentId.value,
      studentCode: studentCode.value,
    })
    incidentSuccess.value = 'Đã xác nhận yêu cầu sửa chữa hoàn thành.'
    await loadIncidents()
  } catch (err) {
    incidentError.value = err.response?.data?.message || 'Không xác nhận được yêu cầu sửa chữa.'
  } finally {
    incidentActionLoading.value = null
  }
}

const openReopenDialog = (incident) => {
  reopenIncidentTarget.value = incident
  reopenNote.value = ''
  reopenError.value = ''
  reopenDialog.value = true
}

const submitReopen = async () => {
  if (!reopenIncidentTarget.value || !reopenNote.value.trim()) {
    reopenError.value = 'Vui lòng nhập lý do cần xử lý lại.'
    return
  }

  try {
    incidentActionLoading.value = reopenIncidentTarget.value.id
    reopenError.value = ''
    incidentError.value = ''
    incidentSuccess.value = ''
    await api.post(`/incidents/${reopenIncidentTarget.value.id}/reopen`, {
      note: reopenNote.value.trim(),
      studentId: studentId.value,
      studentCode: studentCode.value,
    })
    reopenDialog.value = false
    incidentSuccess.value = 'Đã gửi lại yêu cầu cho nhân viên phụ trách.'
    await loadIncidents()
  } catch (err) {
    reopenError.value = err.response?.data?.message || 'Không thể mở lại yêu cầu sửa chữa.'
  } finally {
    incidentActionLoading.value = null
  }
}

const canCancelIncident = (incident) =>
  ['new', 'accepted', 'assigned', 'reopened'].includes(String(incident.status || '').toLowerCase())

const cancelIncident = async (incident) => {
  if (!window.confirm('Bạn chắc chắn muốn hủy yêu cầu sửa chữa này?'))
    return

  try {
    incidentActionLoading.value = incident.id
    incidentError.value = ''
    incidentSuccess.value = ''
    await api.post(`/incidents/${incident.id}/cancel`, {
      note: 'Sinh viên hủy yêu cầu sửa chữa.',
      studentId: studentId.value,
      studentCode: studentCode.value,
    })
    incidentSuccess.value = 'Đã hủy yêu cầu sửa chữa.'
    await loadIncidents()
  } catch (err) {
    incidentError.value = err.response?.data?.message ||
      ([401, 403].includes(err.response?.status)
        ? 'Bạn không có quyền hủy yêu cầu sửa chữa này.'
        : 'Không hủy được yêu cầu sửa chữa.')
  } finally {
    incidentActionLoading.value = null
  }
}

const submitRegistration = async () => {
  if (!canSubmitRegistration.value) {
    registrationError.value = registrationBlockReason.value || 'Chưa đủ điều kiện gửi đăng ký nội trú.'
    return
  }

  if (!student.value) {
    registrationError.value = 'Chưa tìm thấy hồ sơ sinh viên để tạo đơn đăng ký.'
    return
  }

  if (new Date(form.endDate) <= new Date(form.startDate)) {
    registrationError.value = 'Ngày kết thúc phải sau ngày bắt đầu.'
    return
  }

  try {
    submitting.value = true
    registrationError.value = ''
    registrationSuccess.value = ''

    await api.post('/registrations', {
      studentId: studentId.value,
      buildingName: form.buildingName,
      roomType: form.roomType,
      priorityType: form.priorityType,
      priorityNote: form.priorityNote,
      startDate: toIsoDate(form.startDate),
      endDate: toIsoDate(form.endDate),
      status: 'Pending',
    })

    registrationSuccess.value = 'Đã gửi đơn đăng ký. Admin/nhân viên có thể duyệt ở mục Duyệt xếp phòng.'
    form.priorityType = ''
    form.priorityNote = ''
    await loadRegistrations()
  } catch (err) {
    registrationError.value = 'Không gửi được đơn đăng ký. Kiểm tra dữ liệu sinh viên và service N2.'
    console.error(err)
  } finally {
    submitting.value = false
  }
}

const openSignDialog = (contract) => {
  signTarget.value = contract
  contractError.value = ''
  contractSuccess.value = ''
  signForm.fullName = student.value?.fullName || displayName.value || ''
  signForm.studentCode = student.value?.studentCode || studentCode.value || ''
  signAccepted.value = Boolean(contract.signedAt)
  hasSignature.value = false
  signDialog.value = true
  nextTick(prepareSignatureCanvas)
}

const submitSignContract = async () => {
  if (!signTarget.value) return

  if (!signAccepted.value || !signForm.fullName.trim() || !signForm.studentCode.trim()) {
    contractError.value = 'Vui lòng xác nhận điều khoản và nhập đầy đủ họ tên, MSSV để ký hợp đồng.'
    return
  }

  if (!signTarget.value.templateFilePath) {
    contractError.value = 'Hợp đồng chưa có file PDF. Vui lòng chờ admin/nhân viên phát hành hợp đồng chuẩn.'
    return
  }

  if (!hasSignature.value || !signatureCanvas.value) {
    contractError.value = 'Vui lòng ký tay trên khung chữ ký trước khi xác nhận hợp đồng.'
    return
  }

  try {
    signing.value = true
    contractError.value = ''
    contractSuccess.value = ''
    await api.post(`/contracts/${signTarget.value.id}/sign`, {
      fullName: signForm.fullName.trim(),
      studentCode: signForm.studentCode.trim(),
      acceptedTerms: true,
      signatureImageDataUrl: signatureCanvas.value.toDataURL('image/png'),
    })
    signDialog.value = false
    success.value = 'Đã ký hợp đồng online thành công. Hệ thống đã tạo bản PDF có chữ ký của bạn.'
    await loadContracts()
  } catch (err) {
    contractError.value = err.response?.data?.message || 'Không ký được hợp đồng online.'
    console.error(err)
  } finally {
    signing.value = false
  }
}

const prepareSignatureCanvas = () => {
  const canvas = signatureCanvas.value

  if (!canvas || signTarget.value?.signedAt) return

  const rect = canvas.getBoundingClientRect()
  const ratio = window.devicePixelRatio || 1
  canvas.width = Math.max(1, Math.floor(rect.width * ratio))
  canvas.height = Math.max(1, Math.floor(rect.height * ratio))

  const context = canvas.getContext('2d')
  context.setTransform(ratio, 0, 0, ratio, 0, 0)
  context.clearRect(0, 0, rect.width, rect.height)
  context.lineWidth = 2.4
  context.lineCap = 'round'
  context.lineJoin = 'round'
  context.strokeStyle = '#0f172a'
}

const signaturePoint = (event) => {
  const canvas = signatureCanvas.value
  const rect = canvas.getBoundingClientRect()

  return {
    x: event.clientX - rect.left,
    y: event.clientY - rect.top,
  }
}

const startSignature = (event) => {
  if (!signatureCanvas.value || signTarget.value?.signedAt) return

  event.preventDefault()
  drawingSignature = true
  lastSignaturePoint = signaturePoint(event)
}

const drawSignature = (event) => {
  if (!drawingSignature || !signatureCanvas.value || !lastSignaturePoint) return

  event.preventDefault()
  const context = signatureCanvas.value.getContext('2d')
  const nextPoint = signaturePoint(event)
  context.beginPath()
  context.moveTo(lastSignaturePoint.x, lastSignaturePoint.y)
  context.lineTo(nextPoint.x, nextPoint.y)
  context.stroke()
  lastSignaturePoint = nextPoint
  hasSignature.value = true
}

const stopSignature = () => {
  drawingSignature = false
  lastSignaturePoint = null
}

const clearSignature = () => {
  const canvas = signatureCanvas.value

  if (!canvas) return

  const rect = canvas.getBoundingClientRect()
  const context = canvas.getContext('2d')
  context.clearRect(0, 0, rect.width, rect.height)
  hasSignature.value = false
}

const openContractPdf = async (contract, kind) => {
  const endpoint = kind === 'signed' ? 'signed-file' : 'template-file'

  try {
    const response = await api.get(`/contracts/${contract.id}/${endpoint}`, {
      responseType: 'blob',
    })
    const blob = new Blob([response.data], { type: 'application/pdf' })
    const url = URL.createObjectURL(blob)
    window.open(url, '_blank', 'noopener')
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (err) {
    const message = err.response?.data?.message ||
      (kind === 'signed' ? 'Chưa có bản PDF đã ký.' : 'Chưa có file PDF mẫu.')
    if (signDialog.value) contractError.value = message
    else error.value = message
    console.error(err)
  }
}

const canSignContract = (contract) => {
  const status = String(contract.status || '').toLowerCase()
  return !contract.signedAt && ['active', 'pendingsignature'].includes(status)
}

const priorityLabel = (value) => {
  return priorityOptions.find((item) => item.value === value)?.label || 'Không ưu tiên'
}

const statusLabel = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'pending') return 'Chờ duyệt'
  if (normalized === 'pendingsignature') return 'Chờ ký online'
  if (normalized === 'approved') return 'Đã duyệt'
  if (normalized === 'rejected') return 'Đã từ chối'
  if (normalized === 'active') return 'Đang hiệu lực'
  if (normalized === 'cancelled') return 'Đã hủy'
  if (normalized === 'expired') return 'Hết hạn'
  return status || 'Chưa cập nhật'
}

const statusClass = (status) => {
  const normalized = String(status || '').toLowerCase()
  return {
    'status-pending': normalized === 'pending' || normalized === 'pendingsignature',
    'status-approved': normalized === 'approved' || normalized === 'active',
    'status-rejected': normalized === 'rejected' || normalized === 'cancelled',
    'status-expired': normalized === 'expired',
  }
}

const incidentStatusLabel = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'new') return 'Mới gửi'
  if (normalized === 'accepted') return 'Đã tiếp nhận'
  if (normalized === 'assigned') return 'Đã phân công'
  if (normalized === 'processing') return 'Đang xử lý'
  if (normalized === 'waiting-materials') return 'Chờ vật tư'
  if (normalized === 'done' || normalized === 'completed') return 'Chờ bạn xác nhận'
  if (normalized === 'confirmed') return 'Đã hoàn thành'
  if (normalized === 'reopened') return 'Đang xử lý lại'
  if (normalized === 'rejected') return 'Từ chối'
  if (normalized === 'cancelled') return 'Đã hủy'
  return status || 'Chưa cập nhật'
}

const incidentPriorityLabel = (priority) =>
  incidentPriorityOptions.find((item) => item.value === priority)?.title || 'Bình thường'

const incidentCategoryLabel = (category) => {
  return incidentCategories.find((item) => item.value === category)?.title || category
}

const roomDisplay = (incident) => {
  const building = String(incident.building || '').trim()
  const roomName = String(incident.roomName || '').trim()

  if (building && roomName)
    return `${building}-${roomName}`

  return roomName ? `Phòng #${roomName}` : '-'
}

const incidentStatusClass = (status) => {
  const normalized = String(status || '').toLowerCase()
  return {
    'status-pending': normalized === 'new',
    'status-approved': ['accepted', 'assigned', 'processing', 'confirmed'].includes(normalized),
    'status-expired': ['waiting-materials', 'completed', 'reopened'].includes(normalized),
    'status-rejected': ['rejected', 'cancelled'].includes(normalized),
  }
}

const formatDate = (value) => {
  if (!value) return '-'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

const formatDateTime = (value) => {
  if (!value) return '-'
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

const money = (value) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(Number(value || 0))
}

setDefaultDates()
onMounted(loadAll)
</script>

<style scoped>
.student-portal {
  display: grid;
  gap: 18px;
}

.incident-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.table-note {
  display: block;
  margin-top: 4px;
  color: var(--muted);
  font-size: 12px;
}

.priority-low { background: #f5f5f5; color: #595959; }
.priority-normal { background: #e6f4ff; color: #0958d9; }
.priority-high { background: #fff7e6; color: #d46b08; }
.priority-urgent { background: #fff1f0; color: #cf1322; }
.dialog-copy { margin: 0 0 14px; color: var(--muted); }

.contract-sign-cell {
  display: grid;
  gap: 5px;
}

.contract-sign-dialog {
  background: #ffffff;
}

.contract-file-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 14px;
}

.dialog-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  border-bottom: 1px solid var(--line);
}

.dialog-title strong {
  display: block;
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 21px;
}

.contract-paper {
  padding: 22px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background:
    linear-gradient(180deg, #ffffff, #fbfdfb),
    #ffffff;
}

.contract-paper-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--line);
}

.contract-paper-head span {
  color: var(--muted);
  font-size: 13px;
  font-weight: 900;
  text-transform: uppercase;
}

.contract-paper-head h3 {
  margin: 6px 0 0;
  color: var(--brand-dark);
  font-family: var(--font-heading);
  font-size: 22px;
  line-height: 1.25;
}

.contract-paper-head > strong {
  padding: 8px 12px;
  border-radius: 8px;
  background: #ecfdf5;
  color: var(--brand-dark);
  white-space: nowrap;
}

.contract-paper-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-top: 16px;
}

.contract-paper-grid div {
  min-height: 82px;
  padding: 13px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.contract-paper-grid span,
.contract-paper-grid strong,
.contract-paper-grid small {
  display: block;
}

.contract-paper-grid span {
  color: var(--muted);
  font-size: 12px;
  font-weight: 900;
}

.contract-paper-grid strong {
  margin-top: 5px;
  color: var(--ink);
}

.contract-paper-grid small {
  margin-top: 4px;
  color: var(--muted);
}

.contract-terms {
  margin-top: 16px;
  padding: 16px;
  border: 1px solid #d9f7be;
  border-radius: 8px;
  background: #f6ffed;
}

.contract-terms strong {
  display: block;
  color: var(--brand-dark);
  margin-bottom: 6px;
}

.contract-terms p {
  margin: 0;
  color: #334155;
  line-height: 1.65;
}

.signature-proof {
  display: grid;
  grid-template-columns: 46px minmax(0, 1fr);
  gap: 12px;
  margin-top: 16px;
  padding: 14px;
  border: 1px solid #bbf7d0;
  border-radius: 8px;
  background: #f0fdf4;
}

.signature-proof .mdi {
  display: grid;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 8px;
  background: #dcfce7;
  color: #15803d;
  font-size: 24px;
}

.signature-proof strong,
.signature-proof small {
  display: block;
}

.signature-proof small {
  margin-top: 4px;
  color: var(--muted);
  word-break: break-all;
}

.signature-form {
  margin-top: 16px;
  padding: 16px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #fbfdfb;
}

.signature-pad-block {
  display: grid;
  gap: 10px;
  margin-top: 12px;
}

.signature-pad-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.signature-pad-head strong,
.signature-pad-head small {
  display: block;
}

.signature-pad-head small {
  margin-top: 3px;
  color: var(--muted);
}

.signature-canvas {
  width: 100%;
  height: 170px;
  border: 1px dashed #94a3b8;
  border-radius: 8px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.96), rgba(248, 250, 252, 0.96)),
    #ffffff;
  cursor: crosshair;
  touch-action: none;
}

.student-hero {
  display: flex;
  justify-content: space-between;
  gap: 28px;
  padding: 28px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: linear-gradient(135deg, #ffffff, #f1fbf6);
}

.student-hero h2 {
  margin: 4px 0 8px;
  color: var(--brand-dark);
  font-size: 30px;
}

.student-hero p {
  max-width: 640px;
  margin: 0;
  color: var(--muted);
  line-height: 1.6;
}

.hero-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(110px, 1fr));
  gap: 12px;
  min-width: 390px;
}

.stat-tile,
.panel {
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.stat-tile {
  display: grid;
  align-content: center;
  gap: 4px;
  min-height: 112px;
  padding: 16px;
}

.stat-tile .mdi {
  color: var(--brand);
  font-size: 26px;
}

.stat-tile strong {
  color: var(--ink);
  font-size: 25px;
}

.stat-tile small {
  color: var(--muted);
  font-weight: 800;
}

.loading-state {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 140px;
  padding: 24px;
  color: var(--muted);
}

.portal-grid {
  display: grid;
  grid-template-columns: minmax(320px, 0.85fr) minmax(420px, 1.15fr);
  gap: 18px;
}

.panel {
  padding: 20px;
}

.panel-title {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.panel-title .mdi {
  display: grid;
  place-items: center;
  width: 40px;
  height: 40px;
  border-radius: 8px;
  background: #eaf8f0;
  color: var(--brand-dark);
  font-size: 22px;
}

.panel-title strong,
.panel-title small {
  display: block;
}

.panel-title strong {
  color: var(--ink);
  font-size: 17px;
}

.panel-title small {
  margin-top: 3px;
  color: var(--muted);
}

.profile-list {
  display: grid;
  gap: 12px;
}

.profile-list div {
  display: grid;
  gap: 4px;
  padding-bottom: 12px;
  border-bottom: 1px solid #eef2f5;
}

.profile-list span {
  color: var(--muted);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.profile-list strong {
  color: var(--ink);
  font-size: 14px;
  line-height: 1.45;
}

.registration-form {
  display: grid;
  gap: 10px;
}

.auto-room-field {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  align-items: center;
  min-height: 56px;
  padding: 8px 12px;
  border: 1px solid #d9e7df;
  border-radius: 8px;
  background: #f6fffa;
}

.auto-room-field > .mdi {
  color: #007a4d;
  font-size: 24px;
}

.auto-room-field small,
.auto-room-field strong {
  display: block;
}

.auto-room-field small {
  color: var(--muted);
  font-size: 12px;
}

.auto-room-field strong {
  margin-top: 2px;
  color: var(--ink);
  overflow-wrap: anywhere;
}

.auto-room-field.is-missing {
  border-color: #ffd8bf;
  background: #fff7e6;
}

.ai-suggestion-panel {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  gap: 12px;
  padding: 14px;
  border: 1px solid #b7eb8f;
  border-radius: 8px;
  background: #f6ffed;
}

.ai-suggestion-panel > .mdi {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 8px;
  background: #d9f7be;
  color: #237804;
  font-size: 24px;
}

.ai-suggestion-panel strong,
.ai-suggestion-panel small {
  display: block;
}

.ai-suggestion-panel p {
  margin: 6px 0;
  color: #31443a;
  line-height: 1.5;
}

.ai-suggestion-panel small {
  color: var(--muted);
  overflow-wrap: anywhere;
}

.form-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.table-wrap {
  overflow-x: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
  min-width: 760px;
}

th,
td {
  padding: 13px 12px;
  border-bottom: 1px solid #eef2f5;
  text-align: left;
  vertical-align: middle;
}

th {
  color: #34495e;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

td {
  color: var(--ink);
  font-size: 14px;
}

.status-pill {
  display: inline-flex;
  align-items: center;
  min-height: 28px;
  padding: 0 10px;
  border-radius: 8px;
  background: #f1f5f9;
  color: #475569;
  font-size: 12px;
  font-weight: 900;
}

.status-pending {
  background: #fff8e6;
  color: #a16207;
}

.status-approved {
  background: #eaf8f0;
  color: #0f7a44;
}

.status-rejected {
  background: #fee2e2;
  color: #b91c1c;
}

.status-expired {
  background: #eef2f7;
  color: #475569;
}

.empty-state,
.empty-cell {
  color: var(--muted);
}

.empty-cell {
  padding: 24px 12px;
  text-align: center;
}

@media (max-width: 1100px) {
  .student-hero,
  .portal-grid {
    grid-template-columns: 1fr;
  }

  .student-hero {
    display: grid;
  }

  .hero-stats {
    min-width: 0;
  }
}

@media (max-width: 720px) {
  .student-hero,
  .panel {
    padding: 16px;
  }

  .hero-stats {
    grid-template-columns: 1fr;
  }

  .student-hero h2 {
    font-size: 24px;
  }
}
</style>

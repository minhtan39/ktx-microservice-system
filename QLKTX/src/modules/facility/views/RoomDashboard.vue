<template>
  <section class="room-dashboard">
    <div class="page-heading">
      <div>
        <span class="page-kicker">Nhóm 1 - Room & Building</span>
        <h2>Quản lý phòng và tòa nhà</h2>
      </div>
      <v-btn
        color="primary"
        variant="flat"
        prepend-icon="mdi-refresh"
        :loading="loading"
        @click="loadAll"
      >
        Làm mới
      </v-btn>
    </div>

    <v-alert v-if="message" :type="messageType" variant="tonal">
      {{ message }}
    </v-alert>

    <v-row>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Tổng phòng</span>
          <strong>{{ roomMetrics.totalRooms }}</strong>
        </v-sheet>
      </v-col>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Còn giường</span>
          <strong>{{ roomMetrics.availableBeds }}</strong>
        </v-sheet>
      </v-col>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Đang ở</span>
          <strong>{{ roomMetrics.occupiedBeds }}</strong>
        </v-sheet>
      </v-col>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Sửa chữa</span>
          <strong>{{ roomMetrics.maintenanceRooms }}</strong>
        </v-sheet>
      </v-col>
    </v-row>

    <v-sheet class="tab-shell">
      <v-tabs v-model="activeTab" color="primary">
        <v-tab value="rooms">Phòng & trạng thái</v-tab>
        <v-tab value="buildings">Tòa nhà</v-tab>
        <v-tab value="roomTypes">Loại phòng</v-tab>
        <v-tab value="map">Sơ đồ tầng</v-tab>
      </v-tabs>
    </v-sheet>

    <div class="tab-content">
      <section v-if="activeTab === 'map'">
        <v-sheet class="panel">
          <div class="panel-toolbar">
            <v-row>
              <v-col cols="12" md="4">
                <v-select
                  v-model="selectedBuilding"
                  :items="buildingFilterOptions"
                  item-title="title"
                  item-value="value"
                  label="Tòa nhà"
                  density="comfortable"
                  variant="outlined"
                  hide-details
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="selectedFloor"
                  :items="floorOptions"
                  item-title="title"
                  item-value="value"
                  label="Tầng"
                  density="comfortable"
                  variant="outlined"
                  hide-details
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="selectedStatus"
                  :items="statusFilterOptions"
                  item-title="title"
                  item-value="value"
                  label="Trạng thái"
                  density="comfortable"
                  variant="outlined"
                  hide-details
                />
              </v-col>
            </v-row>
          </div>

          <v-progress-linear v-if="loading" indeterminate color="primary" />

          <v-row>
            <v-col
              v-for="room in filteredRooms"
              :key="room.roomId"
              cols="12"
              sm="6"
              md="4"
              lg="3"
            >
              <v-card
                clickable
                :border="true"
                class="room-card elevation-2"
                @click="openRoomDetail(room)"
              >
                <v-sheet :color="getStatusColor(room.status)" height="8" />

                <v-card-item>
                  <div class="room-card-head">
                    <div>
                      <span class="room-title">Phòng {{ room.roomNumber }}</span>
                      <small>{{ room.buildingDisplayName }} - {{ room.floorName }}</small>
                    </div>
                    <v-chip size="small" :color="getStatusColor(room.status)">
                      {{ getRoomStatusText(room) }}
                    </v-chip>
                  </div>

                  <div class="room-meta">
                    <span>
                      <v-icon size="small">mdi-account-group</v-icon>
                      {{ room.occupiedBeds }}/{{ room.capacity }} sinh viên
                    </span>
                    <span>
                      <v-icon size="small">mdi-bed</v-icon>
                      Còn {{ room.availableBeds }} giường
                    </span>
                    <span>
                      <v-icon size="small">mdi-home-city-outline</v-icon>
                      {{ room.roomType }} - {{ room.genderText }}
                    </span>
                    <span>
                      <v-icon size="small">mdi-cash</v-icon>
                      {{ formatPrice(room.monthlyFee) }}/tháng
                    </span>
                  </div>
                </v-card-item>
              </v-card>
            </v-col>

            <v-col v-if="!loading && filteredRooms.length === 0" cols="12">
              <v-sheet class="empty-state">Không có phòng phù hợp.</v-sheet>
            </v-col>
          </v-row>
        </v-sheet>
      </section>

      <section v-else-if="activeTab === 'buildings'">
        <v-sheet class="panel">
          <div class="panel-head">
            <h3>Tòa nhà</h3>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateBuilding">
              Thêm tòa
            </v-btn>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Mã tòa</th>
                <th>Tên hiển thị</th>
                <th>Số tầng</th>
                <th>Số phòng</th>
                <th>Giường trống</th>
                <th>Vận hành</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="building in buildings" :key="building.buildingName">
                <td>{{ building.buildingName }}</td>
                <td>{{ building.displayName }}</td>
                <td>{{ building.floors }}</td>
                <td>{{ building.totalRooms }}</td>
                <td>{{ building.availableBeds }}</td>
                <td>
                  <div class="building-status-cell">
                    <v-chip size="small" variant="tonal" :color="getBuildingStatusColor(building.operationalStatus)">
                      {{ building.operationalStatusText }}
                    </v-chip>
                    <small v-if="building.activeNoticeCount > 0">
                      {{ building.activeNoticeCount }} thông báo đang hiệu lực
                    </small>
                    <small v-else>Không có cảnh báo</small>
                  </div>
                </td>
                <td>
                  <div class="row-actions">
                    <v-btn icon="mdi-eye" size="small" variant="text" @click="openBuildingDetail(building)" />
                    <v-btn icon="mdi-bell-plus-outline" size="small" variant="text" color="warning" @click="openCreateBuildingNotice(building)" />
                    <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditBuilding(building)" />
                    <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="deleteBuilding(building)" />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </v-sheet>
      </section>

      <section v-else-if="activeTab === 'roomTypes'">
        <v-sheet class="panel">
          <div class="panel-head">
            <h3>Loại phòng</h3>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateRoomType">
              Thêm loại phòng
            </v-btn>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Loại phòng</th>
                <th>Số giường tối đa</th>
                <th>Đơn giá</th>
                <th>Số phòng</th>
                <th>Tiện nghi mặc định</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="roomType in roomTypes" :key="roomType.roomType">
                <td>{{ roomType.roomType }}</td>
                <td>{{ roomType.capacity }}</td>
                <td>{{ formatPrice(roomType.monthlyFee) }}</td>
                <td>{{ roomType.totalRooms }}</td>
                <td>{{ roomType.amenities }}</td>
                <td>
                  <div class="row-actions">
                    <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditRoomType(roomType)" />
                    <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="deleteRoomType(roomType)" />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </v-sheet>
      </section>

      <section v-else>
        <v-sheet class="panel">
          <div class="panel-head">
            <h3>Phòng & trạng thái</h3>
            <div class="room-tools">
              <v-text-field
                v-model="roomSearch"
                prepend-inner-icon="mdi-magnify"
                label="Tìm phòng, tòa, loại, trạng thái"
                density="compact"
                variant="outlined"
                hide-details
              />
              <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateRoom">
                Thêm phòng
              </v-btn>
            </div>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Phòng</th>
                <th>Tòa / tầng</th>
                <th>Loại</th>
                <th>Giới tính</th>
                <th>Sức chứa</th>
                <th>Giá</th>
                <th>Trạng thái tự động</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="room in searchedRooms" :key="room.roomId">
                <td>{{ room.roomNumber }}</td>
                <td>{{ room.buildingDisplayName }} / {{ room.floorName }}</td>
                <td>{{ room.roomType }}</td>
                <td>{{ room.genderText }}</td>
                <td>
                  <div class="occupancy-cell">
                    <strong>{{ room.occupiedBeds }}/{{ room.capacity }}</strong>
                    <small>Còn {{ room.availableBeds }} giường</small>
                  </div>
                </td>
                <td>{{ formatPrice(room.monthlyFee) }}</td>
                <td>
                  <div class="auto-status">
                    <v-chip size="small" variant="tonal" :color="getStatusColor(room.status)">
                      {{ getRoomStatusText(room) }}
                    </v-chip>
                    <small>Theo duyệt xếp phòng N2</small>
                  </div>
                </td>
                <td>
                  <div class="row-actions">
                    <v-btn icon="mdi-eye" size="small" variant="text" @click="openRoomDetail(room)" />
                    <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditRoom(room)" />
                    <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="deleteRoom(room)" />
                  </div>
                </td>
              </tr>
              <tr v-if="!loading && searchedRooms.length === 0">
                <td colspan="8" class="table-empty">Không có phòng phù hợp.</td>
              </tr>
            </tbody>
          </table>
        </v-sheet>
      </section>
    </div>

    <v-dialog v-model="buildingDialog" max-width="560px">
      <v-card>
        <v-card-title>{{ editingBuildingName ? 'Sửa tòa nhà' : 'Thêm tòa nhà' }}</v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="buildingForm.buildingName"
                label="Mã tòa"
                density="compact"
                :disabled="Boolean(editingBuildingName)"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model="buildingForm.displayName" label="Tên hiển thị" density="compact" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model.number="buildingForm.floors" label="Số tầng" type="number" density="compact" />
            </v-col>
            <v-col cols="12">
              <v-textarea v-model="buildingForm.description" label="Ghi chú" rows="2" density="compact" />
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="buildingDialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveBuilding">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="buildingDetailDialog" max-width="760px">
      <v-card v-if="selectedBuildingDetail">
        <v-card-title class="building-detail-title">
          <div>
            <span>{{ selectedBuildingDetail.displayName }}</span>
            <small>{{ selectedBuildingDetail.totalRooms }} phòng · còn {{ selectedBuildingDetail.availableBeds }} giường</small>
          </div>
          <v-chip :color="getBuildingStatusColor(selectedBuildingDetail.operationalStatus)" variant="tonal">
            {{ selectedBuildingDetail.operationalStatusText }}
          </v-chip>
        </v-card-title>
        <v-card-text>
          <div class="building-summary">
            <p><strong>Mã tòa:</strong> {{ selectedBuildingDetail.buildingName }}</p>
            <p><strong>Số tầng:</strong> {{ selectedBuildingDetail.floors }}</p>
            <p><strong>Mô tả:</strong> {{ selectedBuildingDetail.description || 'Chưa có mô tả.' }}</p>
          </div>

          <v-divider class="my-4" />

          <div class="panel-head compact-head">
            <h3>Thông báo vận hành</h3>
            <v-btn size="small" color="primary" prepend-icon="mdi-bell-plus-outline" @click="openCreateBuildingNotice(selectedBuildingDetail)">
              Thêm thông báo
            </v-btn>
          </div>

          <div v-if="selectedBuildingDetail.facilityNotices.length > 0" class="notice-list">
            <v-sheet
              v-for="notice in selectedBuildingDetail.facilityNotices"
              :key="notice.id"
              class="notice-item"
              :class="`notice-${notice.severity.toLowerCase()}`"
            >
              <div class="notice-main">
                <div>
                  <div class="notice-title-row">
                    <strong>{{ notice.title }}</strong>
                    <v-chip size="x-small" :color="getNoticeSeverityColor(notice.severity)" variant="tonal">
                      {{ notice.severityText }}
                    </v-chip>
                  </div>
                  <small>{{ notice.areaName }} · {{ notice.categoryText }} · {{ notice.statusText }}</small>
                </div>
                <div class="row-actions">
                  <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditBuildingNotice(selectedBuildingDetail, notice)" />
                  <v-btn icon="mdi-check-circle-outline" size="small" variant="text" color="success" @click="resolveBuildingNotice(selectedBuildingDetail, notice)" />
                  <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="deleteBuildingNotice(selectedBuildingDetail, notice)" />
                </div>
              </div>
              <p>{{ notice.description || 'Không có mô tả chi tiết.' }}</p>
              <small class="notice-time">
                Bắt đầu {{ formatDateTime(notice.startedAt) }}
                <template v-if="notice.expectedResolvedAt"> · Dự kiến xong {{ formatDateTime(notice.expectedResolvedAt) }}</template>
              </small>
            </v-sheet>
          </div>
          <v-alert v-else type="success" variant="tonal" density="comfortable">
            Tòa đang vận hành ổn định, chưa có thông báo bảo trì/bảo dưỡng.
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn color="primary" variant="text" @click="buildingDetailDialog = false">Đóng</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="buildingNoticeDialog" max-width="720px">
      <v-card>
        <v-card-title>
          {{ editingBuildingNoticeId ? 'Sửa thông báo vận hành' : 'Thêm thông báo vận hành' }}
        </v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="4">
              <v-text-field v-model="buildingNoticeForm.buildingName" label="Tòa" density="compact" readonly />
            </v-col>
            <v-col cols="12" md="8">
              <v-text-field v-model="buildingNoticeForm.areaName" label="Khu vực/hạng mục" density="compact" placeholder="VD: Thang máy A01" />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="buildingNoticeForm.category"
                :items="noticeCategoryOptions"
                item-title="title"
                item-value="value"
                label="Loại hạng mục"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="buildingNoticeForm.status"
                :items="noticeStatusOptions"
                item-title="title"
                item-value="value"
                label="Trạng thái"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="buildingNoticeForm.severity"
                :items="noticeSeverityOptions"
                item-title="title"
                item-value="value"
                label="Mức độ"
                density="compact"
              />
            </v-col>
            <v-col cols="12">
              <v-text-field v-model="buildingNoticeForm.title" label="Tiêu đề sinh viên sẽ thấy" density="compact" />
            </v-col>
            <v-col cols="12">
              <v-textarea v-model="buildingNoticeForm.description" label="Mô tả chi tiết" rows="3" density="compact" />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field v-model="buildingNoticeForm.startedAt" label="Bắt đầu" type="datetime-local" density="compact" />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field v-model="buildingNoticeForm.expectedResolvedAt" label="Dự kiến hoàn thành" type="datetime-local" density="compact" />
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="buildingNoticeDialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveBuildingNotice">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="roomTypeDialog" max-width="620px">
      <v-card>
        <v-card-title>{{ editingRoomType ? 'Sửa loại phòng' : 'Thêm loại phòng' }}</v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="roomTypeForm.roomType"
                label="Loại phòng"
                density="compact"
                :disabled="Boolean(editingRoomType)"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model.number="roomTypeForm.capacity" label="Số giường tối đa" type="number" density="compact" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model.number="roomTypeForm.monthlyFee" label="Đơn giá tháng" type="number" density="compact" />
            </v-col>
            <v-col cols="12">
              <v-text-field v-model="roomTypeForm.amenities" label="Tiện nghi mặc định" density="compact" />
            </v-col>
            <v-col cols="12">
              <v-textarea v-model="roomTypeForm.description" label="Mô tả" rows="2" density="compact" />
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="roomTypeDialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveRoomType">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="roomDialog" max-width="720px">
      <v-card>
        <v-card-title>{{ editingRoomId ? 'Sửa phòng' : 'Thêm phòng' }}</v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="roomForm.roomId"
                label="ID phòng"
                type="number"
                density="compact"
                :disabled="Boolean(editingRoomId)"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model="roomForm.roomNumber" label="Số phòng" density="compact" />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="roomForm.buildingName"
                :items="buildingOptions"
                item-title="title"
                item-value="value"
                label="Tòa"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model.number="roomForm.floor" label="Tầng" type="number" density="compact" />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="roomForm.roomType"
                :items="roomTypeOptions"
                item-title="title"
                item-value="value"
                label="Loại phòng"
                density="compact"
                @update:model-value="applySelectedRoomType"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="roomForm.gender"
                :items="genderOptions"
                item-title="title"
                item-value="value"
                label="Giới tính"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="roomForm.capacity"
                label="Số giường tối đa"
                type="number"
                density="compact"
                readonly
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="roomForm.monthlyFee"
                label="Đơn giá"
                type="number"
                density="compact"
                readonly
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field v-model="roomForm.amenities" label="Tiện nghi" density="compact" />
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="roomDialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveRoom">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="detailDialog" max-width="620px">
      <v-card v-if="selectedRoom">
        <v-card-title class="bg-primary text-white">
          Chi tiết phòng {{ selectedRoom.roomNumber }}
        </v-card-title>
        <v-card-text class="room-dialog">
          <p><strong>Tòa:</strong> {{ selectedRoom.buildingDisplayName }}</p>
          <p><strong>Tầng:</strong> {{ selectedRoom.floorName }}</p>
          <p><strong>Loại phòng:</strong> {{ selectedRoom.roomType }}</p>
          <p><strong>Giới tính:</strong> {{ selectedRoom.genderText }}</p>
          <p><strong>Trạng thái:</strong> {{ getRoomStatusText(selectedRoom) }}</p>
          <p><strong>Sức chứa:</strong> {{ selectedRoom.occupiedBeds }}/{{ selectedRoom.capacity }} sinh viên</p>
          <p><strong>Giá phòng:</strong> {{ formatPrice(selectedRoom.monthlyFee) }}/tháng</p>
          <p><strong>Tiện nghi:</strong> {{ selectedRoom.amenities }}</p>

          <v-divider class="my-4" />

          <p class="font-weight-bold mb-2">Sinh viên đang ở (Reference IDs)</p>
          <v-list v-if="selectedRoom.occupancyReferences.length > 0" density="compact">
            <v-list-item
              v-for="reference in selectedRoom.occupancyReferences"
              :key="`${reference.studentId}-${reference.registrationId}`"
              prepend-icon="mdi-account"
            >
              <div class="reference-row">
                <span>SV #{{ reference.studentId }}</span>
                <small>Đơn #{{ reference.registrationId }} - {{ reference.contractCode }}</small>
              </div>
            </v-list-item>
          </v-list>
          <v-alert v-else type="info" variant="tonal" density="compact">
            Phòng chưa có sinh viên được xếp từ N2.
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn color="primary" variant="text" @click="detailDialog = false">Đóng</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import api from '@/services/api'

const ALL_VALUE = 'all'

const activeTab = ref('rooms')
const loading = ref(false)
const saving = ref(false)
const message = ref('')
const messageType = ref('success')
const roomSearch = ref('')
let refreshTimer = null

const rooms = ref([])
const buildings = ref([])
const roomTypes = ref([])

const selectedBuilding = ref(ALL_VALUE)
const selectedFloor = ref(ALL_VALUE)
const selectedStatus = ref(ALL_VALUE)
const selectedRoom = ref(null)
const selectedBuildingDetail = ref(null)

const buildingDialog = ref(false)
const buildingDetailDialog = ref(false)
const buildingNoticeDialog = ref(false)
const roomTypeDialog = ref(false)
const roomDialog = ref(false)
const detailDialog = ref(false)

const editingBuildingName = ref('')
const editingBuildingNoticeId = ref(null)
const editingRoomType = ref('')
const editingRoomId = ref(null)

function toDateTimeInput(value) {
  if (!value) return ''
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return localDate.toISOString().slice(0, 16)
}

function fromDateTimeInput(value) {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

const emptyBuildingForm = () => ({
  buildingName: '',
  displayName: '',
  floors: 1,
  description: '',
})

const emptyBuildingNoticeForm = (building = null) => ({
  buildingName: building?.buildingName || '',
  areaName: '',
  category: 'Other',
  status: 'Notice',
  severity: 'Info',
  title: '',
  description: '',
  startedAt: toDateTimeInput(new Date()),
  expectedResolvedAt: '',
  isActive: true,
})

const emptyRoomTypeForm = () => ({
  roomType: '',
  capacity: 4,
  monthlyFee: 0,
  description: '',
  amenities: '',
})

const emptyRoomForm = () => {
  const firstBuilding = buildings.value[0]?.buildingName || ''
  const firstRoomType = roomTypes.value[0]
  const roomId = rooms.value.reduce(
    (max, room) => Math.max(max, Number(room.roomId || 0)),
    0,
  ) + 1

  return {
    roomId,
    roomNumber: String(roomId),
    buildingName: firstBuilding,
    floor: 1,
    roomType: firstRoomType?.roomType || '',
    gender: true,
    capacity: firstRoomType?.capacity || 4,
    monthlyFee: firstRoomType?.monthlyFee || 0,
    amenities: firstRoomType?.amenities || '',
  }
}

const buildingForm = ref(emptyBuildingForm())
const buildingNoticeForm = ref(emptyBuildingNoticeForm())
const roomTypeForm = ref(emptyRoomTypeForm())
const roomForm = ref(emptyRoomForm())

const statusOptions = [
  { title: 'Còn giường', value: 'Available' },
  { title: 'Đầy', value: 'Full' },
  { title: 'Đang bảo trì', value: 'Maintenance' },
]

const statusFilterOptions = computed(() => [
  { title: 'Tất cả trạng thái', value: ALL_VALUE },
  ...statusOptions,
])

const genderOptions = [
  { title: 'Nam', value: true },
  { title: 'Nữ', value: false },
]

const noticeCategoryOptions = [
  { title: 'Thang máy', value: 'Elevator' },
  { title: 'Thang bộ', value: 'Stair' },
  { title: 'Phòng học/tự học', value: 'LearningRoom' },
  { title: 'Điện', value: 'Electricity' },
  { title: 'Nước', value: 'Water' },
  { title: 'An toàn', value: 'Safety' },
  { title: 'Khác', value: 'Other' },
]

const noticeStatusOptions = [
  { title: 'Thông báo', value: 'Notice' },
  { title: 'Tạm dừng sử dụng', value: 'OutOfService' },
  { title: 'Đang bảo trì', value: 'Maintenance' },
  { title: 'Đang thay thế thiết bị', value: 'Replacing' },
  { title: 'Đang kiểm tra', value: 'Inspection' },
  { title: 'Đã hoàn thành', value: 'Resolved' },
]

const noticeSeverityOptions = [
  { title: 'Thông tin', value: 'Info' },
  { title: 'Cần chú ý', value: 'Warning' },
  { title: 'Khẩn cấp', value: 'Critical' },
]

const buildingOptions = computed(() =>
  buildings.value.map((building) => ({
    title: building.displayName,
    value: building.buildingName,
  })),
)

const buildingFilterOptions = computed(() => [
  { title: 'Tất cả tòa', value: ALL_VALUE },
  ...buildingOptions.value,
])

const roomTypeOptions = computed(() =>
  roomTypes.value.map((roomType) => ({
    title: `${roomType.roomType} - ${formatPrice(roomType.monthlyFee)}`,
    value: roomType.roomType,
  })),
)

const floorOptions = computed(() => {
  const floors = new Set()

  if (selectedBuilding.value !== ALL_VALUE) {
    const building = buildings.value.find((item) => item.buildingName === selectedBuilding.value)
    for (let floor = 1; floor <= Number(building?.floors || 0); floor += 1) {
      floors.add(floor)
    }
  }

  rooms.value
    .filter((room) => selectedBuilding.value === ALL_VALUE || room.buildingName === selectedBuilding.value)
    .forEach((room) => floors.add(room.floor))

  return [
    { title: 'Tất cả tầng', value: ALL_VALUE },
    ...Array.from(floors)
      .sort((first, second) => first - second)
      .map((floor) => ({ title: `Tầng ${floor}`, value: floor })),
  ]
})

const filteredRooms = computed(() => {
  return rooms.value.filter((room) => {
    const matchBuilding =
      selectedBuilding.value === ALL_VALUE ||
      room.buildingName === selectedBuilding.value
    const matchFloor =
      selectedFloor.value === ALL_VALUE ||
      room.floor === selectedFloor.value
    const matchStatus =
      selectedStatus.value === ALL_VALUE ||
      room.status === selectedStatus.value

    return matchBuilding && matchFloor && matchStatus
  })
})

const searchedRooms = computed(() => {
  const keyword = roomSearch.value.trim().toLowerCase()
  if (!keyword) return rooms.value

  return rooms.value.filter((room) => {
    const searchableText = [
      room.roomNumber,
      room.buildingName,
      room.buildingDisplayName,
      room.floorName,
      room.roomType,
      room.genderText,
      room.status,
      getRoomStatusText(room),
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()

    return searchableText.includes(keyword)
  })
})

const roomMetrics = computed(() => {
  return rooms.value.reduce(
    (summary, room) => {
      summary.totalRooms += 1
      summary.occupiedBeds += room.occupiedBeds
      summary.availableBeds += room.availableBeds

      if (room.status === 'Maintenance') {
        summary.maintenanceRooms += 1
      }

      return summary
    },
    {
      totalRooms: 0,
      occupiedBeds: 0,
      availableBeds: 0,
      maintenanceRooms: 0,
    },
  )
})

watch(selectedBuilding, () => {
  selectedFloor.value = ALL_VALUE
})

const showMessage = (text, type = 'success') => {
  message.value = text
  messageType.value = type
}

const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.value)) return payload.value
  return []
}

const loadAll = async ({ silent = false } = {}) => {
  try {
    if (!silent) {
      loading.value = true
    }
    const [buildingResponse, roomTypeResponse, roomResponse] = await Promise.all([
      api.get('/buildings'),
      api.get('/room-types'),
      api.get('/rooms'),
    ])

    buildings.value = normalizeList(buildingResponse.data).map(normalizeBuilding)
    roomTypes.value = normalizeList(roomTypeResponse.data)
    rooms.value = normalizeList(roomResponse.data).map(normalizeRoom)

    if (selectedBuildingDetail.value) {
      selectedBuildingDetail.value =
        buildings.value.find((building) => building.buildingName === selectedBuildingDetail.value.buildingName) ||
        selectedBuildingDetail.value
    }
  } catch (error) {
    if (!silent) {
      showMessage('Không tải được dữ liệu RoomService.', 'error')
    }
    console.error(error)
  } finally {
    if (!silent) {
      loading.value = false
    }
  }
}

const normalizeBuilding = (building) => {
  const notices = normalizeList(building.facilityNotices).map(normalizeBuildingNotice)
  const activeNoticeCount = Number(
    building.activeNoticeCount ??
    notices.filter((notice) => notice.isActive && notice.status !== 'Resolved').length,
  )
  const criticalNoticeCount = Number(
    building.criticalNoticeCount ??
    notices.filter((notice) => notice.severity === 'Critical').length,
  )
  const operationalStatus = building.operationalStatus || deriveBuildingOperationalStatus(notices)

  return {
    ...building,
    floors: Number(building.floors ?? 0),
    totalRooms: Number(building.totalRooms ?? 0),
    availableBeds: Number(building.availableBeds ?? 0),
    activeNoticeCount,
    criticalNoticeCount,
    operationalStatus,
    operationalStatusText: building.operationalStatusText || getBuildingStatusText(operationalStatus),
    facilityNotices: notices,
  }
}

const normalizeBuildingNotice = (notice) => ({
  ...notice,
  id: Number(notice.id ?? 0),
  buildingName: notice.buildingName || '',
  areaName: notice.areaName || '',
  category: notice.category || 'Other',
  categoryText: notice.categoryText || getNoticeCategoryText(notice.category),
  status: notice.status || 'Notice',
  statusText: notice.statusText || getNoticeStatusText(notice.status),
  severity: notice.severity || 'Info',
  severityText: notice.severityText || getNoticeSeverityText(notice.severity),
  title: notice.title || '',
  description: notice.description || '',
  startedAt: notice.startedAt || '',
  expectedResolvedAt: notice.expectedResolvedAt || '',
  resolvedAt: notice.resolvedAt || '',
  updatedAt: notice.updatedAt || '',
  isActive: notice.isActive !== false && notice.status !== 'Resolved',
})

const normalizeRoom = (room) => {
  const capacity = Number(room.capacity ?? 0)
  const occupiedBeds = Number(room.occupiedBeds ?? room.currentOccupancy ?? 0)
  const availableBeds = Number(room.availableBeds ?? Math.max(capacity - occupiedBeds, 0))
  const buildingName = String(room.buildingName ?? '').replace(/^Tòa\s*/i, '')
  const status = normalizeStatus(room.status, availableBeds, occupiedBeds, capacity)

  return {
    ...room,
    roomId: Number(room.roomId ?? room.id ?? 0),
    roomNumber: String(room.roomNumber ?? room.name ?? room.roomId ?? ''),
    buildingName,
    buildingDisplayName: room.buildingDisplayName || `Tòa ${buildingName}`,
    floor: Number(room.floor ?? 1),
    floorName: room.floorName || `Tầng ${room.floor ?? 1}`,
    roomType: room.roomType ?? '',
    gender: parseGender(room.gender),
    genderText: room.genderText || (parseGender(room.gender) ? 'Nam' : 'Nữ'),
    capacity,
    occupiedBeds,
    availableBeds,
    monthlyFee: Number(room.monthlyFee ?? room.price ?? 0),
    status,
    amenities: room.amenities || '',
    occupancyReferences: Array.isArray(room.occupancyReferences) ? room.occupancyReferences : [],
  }
}

const parseGender = (value) => {
  if (typeof value === 'boolean') return value
  const normalized = String(value || '').toLowerCase()
  return normalized === 'true' || normalized === 'nam' || normalized === 'male'
}

const normalizeStatus = (status, availableBeds = 0, occupiedBeds = 0, capacity = 0) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized.includes('maintenance') || normalized.includes('bảo trì') || normalized.includes('sửa')) return 'Maintenance'
  if (capacity > 0 && occupiedBeds >= capacity) return 'Full'
  return availableBeds > 0 ? 'Available' : 'Full'
}

const openRoomDetail = (room) => {
  selectedRoom.value = room
  detailDialog.value = true
}

const openCreateBuilding = () => {
  editingBuildingName.value = ''
  buildingForm.value = emptyBuildingForm()
  buildingDialog.value = true
}

const openEditBuilding = (building) => {
  editingBuildingName.value = building.buildingName
  buildingForm.value = {
    buildingName: building.buildingName,
    displayName: building.displayName,
    floors: building.floors,
    description: building.description || '',
  }
  buildingDialog.value = true
}

const saveBuilding = async () => {
  try {
    saving.value = true
    if (editingBuildingName.value) {
      await api.put(`/buildings/${encodeURIComponent(editingBuildingName.value)}`, buildingForm.value)
      showMessage('Đã cập nhật tòa nhà.')
    } else {
      await api.post('/buildings', buildingForm.value)
      showMessage('Đã thêm tòa nhà.')
    }
    buildingDialog.value = false
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không lưu được tòa nhà.', 'error')
    console.error(error)
  } finally {
    saving.value = false
  }
}

const deleteBuilding = async (building) => {
  if (!window.confirm(`Xóa ${building.displayName}?`)) return

  try {
    await api.delete(`/buildings/${encodeURIComponent(building.buildingName)}`)
    showMessage('Đã xóa tòa nhà.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không xóa được tòa nhà.', 'error')
    console.error(error)
  }
}

const openBuildingDetail = (building) => {
  selectedBuildingDetail.value = building
  buildingDetailDialog.value = true
}

const openCreateBuildingNotice = (building) => {
  selectedBuildingDetail.value = building
  editingBuildingNoticeId.value = null
  buildingNoticeForm.value = emptyBuildingNoticeForm(building)
  buildingNoticeDialog.value = true
}

const openEditBuildingNotice = (building, notice) => {
  selectedBuildingDetail.value = building
  editingBuildingNoticeId.value = notice.id
  buildingNoticeForm.value = {
    buildingName: building.buildingName,
    areaName: notice.areaName || '',
    category: notice.category || 'Other',
    status: notice.status || 'Notice',
    severity: notice.severity || 'Info',
    title: notice.title || '',
    description: notice.description || '',
    startedAt: toDateTimeInput(notice.startedAt),
    expectedResolvedAt: toDateTimeInput(notice.expectedResolvedAt),
    isActive: notice.isActive !== false,
  }
  buildingNoticeDialog.value = true
}

const saveBuildingNotice = async () => {
  try {
    saving.value = true
    const buildingName = buildingNoticeForm.value.buildingName
    const payload = {
      ...buildingNoticeForm.value,
      startedAt: fromDateTimeInput(buildingNoticeForm.value.startedAt),
      expectedResolvedAt: fromDateTimeInput(buildingNoticeForm.value.expectedResolvedAt),
      isActive: buildingNoticeForm.value.status !== 'Resolved',
    }

    if (editingBuildingNoticeId.value) {
      await api.put(
        `/buildings/${encodeURIComponent(buildingName)}/facility-notices/${editingBuildingNoticeId.value}`,
        payload,
      )
      showMessage('Đã cập nhật thông báo vận hành.')
    } else {
      await api.post(`/buildings/${encodeURIComponent(buildingName)}/facility-notices`, payload)
      showMessage('Đã thêm thông báo vận hành.')
    }

    buildingNoticeDialog.value = false
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không lưu được thông báo vận hành.', 'error')
    console.error(error)
  } finally {
    saving.value = false
  }
}

const resolveBuildingNotice = async (building, notice) => {
  if (!window.confirm(`Đánh dấu hoàn thành: ${notice.title}?`)) return

  try {
    await api.patch(`/buildings/${encodeURIComponent(building.buildingName)}/facility-notices/${notice.id}/resolve`)
    showMessage('Đã đánh dấu thông báo là hoàn thành.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không hoàn tất được thông báo.', 'error')
    console.error(error)
  }
}

const deleteBuildingNotice = async (building, notice) => {
  if (!window.confirm(`Xóa thông báo: ${notice.title}?`)) return

  try {
    await api.delete(`/buildings/${encodeURIComponent(building.buildingName)}/facility-notices/${notice.id}`)
    showMessage('Đã xóa thông báo vận hành.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không xóa được thông báo.', 'error')
    console.error(error)
  }
}

const openCreateRoomType = () => {
  editingRoomType.value = ''
  roomTypeForm.value = emptyRoomTypeForm()
  roomTypeDialog.value = true
}

const openEditRoomType = (roomType) => {
  editingRoomType.value = roomType.roomType
  roomTypeForm.value = {
    roomType: roomType.roomType,
    capacity: roomType.capacity,
    monthlyFee: roomType.monthlyFee,
    description: roomType.description || '',
    amenities: roomType.amenities || '',
  }
  roomTypeDialog.value = true
}

const saveRoomType = async () => {
  try {
    saving.value = true
    if (editingRoomType.value) {
      await api.put(`/room-types/${encodeURIComponent(editingRoomType.value)}`, roomTypeForm.value)
      showMessage('Đã cập nhật loại phòng.')
    } else {
      await api.post('/room-types', roomTypeForm.value)
      showMessage('Đã thêm loại phòng.')
    }
    roomTypeDialog.value = false
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không lưu được loại phòng.', 'error')
    console.error(error)
  } finally {
    saving.value = false
  }
}

const deleteRoomType = async (roomType) => {
  if (!window.confirm(`Xóa loại phòng ${roomType.roomType}?`)) return

  try {
    await api.delete(`/room-types/${encodeURIComponent(roomType.roomType)}`)
    showMessage('Đã xóa loại phòng.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không xóa được loại phòng.', 'error')
    console.error(error)
  }
}

const openCreateRoom = () => {
  editingRoomId.value = null
  roomForm.value = emptyRoomForm()
  roomDialog.value = true
}

const openEditRoom = (room) => {
  editingRoomId.value = room.roomId
  roomForm.value = {
    roomId: room.roomId,
    roomNumber: room.roomNumber,
    buildingName: room.buildingName,
    floor: room.floor,
    roomType: room.roomType,
    gender: room.gender,
    capacity: room.capacity,
    monthlyFee: room.monthlyFee,
    amenities: room.amenities || '',
  }
  roomDialog.value = true
}

const applySelectedRoomType = () => {
  const selectedType = roomTypes.value.find((item) => item.roomType === roomForm.value.roomType)
  if (!selectedType) return

  roomForm.value.capacity = selectedType.capacity
  roomForm.value.monthlyFee = selectedType.monthlyFee
  roomForm.value.amenities = selectedType.amenities || roomForm.value.amenities
}

const saveRoom = async () => {
  try {
    saving.value = true
    const payload = {
      ...roomForm.value,
      roomId: Number(roomForm.value.roomId),
      floor: Number(roomForm.value.floor),
      capacity: Number(roomForm.value.capacity),
      monthlyFee: Number(roomForm.value.monthlyFee),
    }
    delete payload.status

    if (editingRoomId.value) {
      await api.put(`/rooms/${editingRoomId.value}`, payload)
      showMessage('Đã cập nhật phòng.')
    } else {
      await api.post('/rooms', payload)
      showMessage('Đã thêm phòng.')
    }

    roomDialog.value = false
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không lưu được phòng.', 'error')
    console.error(error)
  } finally {
    saving.value = false
  }
}

const deleteRoom = async (room) => {
  if (!window.confirm(`Xóa phòng ${room.roomNumber}?`)) return

  try {
    await api.delete(`/rooms/${room.roomId}`)
    showMessage('Đã xóa phòng.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không xóa được phòng.', 'error')
    console.error(error)
  }
}

const deriveBuildingOperationalStatus = (notices) => {
  const activeNotices = notices.filter((notice) => notice.isActive && notice.status !== 'Resolved')
  if (activeNotices.some((notice) => notice.severity === 'Critical' || notice.status === 'OutOfService')) return 'Interrupted'
  if (activeNotices.some((notice) => ['Maintenance', 'Replacing'].includes(notice.status))) return 'Maintenance'
  return activeNotices.length > 0 ? 'Notice' : 'Normal'
}

const getBuildingStatusText = (status) => {
  if (status === 'Interrupted') return 'Có hạng mục cần chú ý'
  if (status === 'Maintenance') return 'Đang bảo trì/bảo dưỡng'
  if (status === 'Notice') return 'Có thông báo vận hành'
  return 'Vận hành ổn định'
}

const getBuildingStatusColor = (status) => {
  if (status === 'Interrupted') return 'error'
  if (status === 'Maintenance') return 'warning'
  if (status === 'Notice') return 'info'
  return 'success'
}

const getNoticeSeverityColor = (severity) => {
  if (severity === 'Critical') return 'error'
  if (severity === 'Warning') return 'warning'
  return 'info'
}

const getNoticeCategoryText = (category) => {
  const option = noticeCategoryOptions.find((item) => item.value === category)
  return option?.title || 'Khác'
}

const getNoticeStatusText = (status) => {
  const option = noticeStatusOptions.find((item) => item.value === status)
  return option?.title || 'Thông báo'
}

const getNoticeSeverityText = (severity) => {
  const option = noticeSeverityOptions.find((item) => item.value === severity)
  return option?.title || 'Thông tin'
}

const formatDateTime = (value) => {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'

  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

const getStatusColor = (status) => {
  if (status === 'Available') return 'success'
  if (status === 'Full') return 'error'
  return 'grey'
}

const getStatusText = (status) => {
  if (status === 'Available') return 'Còn giường'
  if (status === 'Full') return 'Đầy'
  return 'Đang bảo trì'
}

const getRoomStatusText = (room) => {
  if (room.status === 'Maintenance') return 'Đang bảo trì'
  if (room.status === 'Full' || Number(room.availableBeds) <= 0) return 'Đầy'
  return Number(room.occupiedBeds) > 0 ? 'Còn giường' : 'Trống'
}

const formatPrice = (value) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(value || 0)
}

onMounted(() => {
  loadAll()
  refreshTimer = window.setInterval(() => loadAll({ silent: true }), 10000)
})

onBeforeUnmount(() => {
  if (refreshTimer) {
    window.clearInterval(refreshTimer)
  }
})
</script>

<style scoped>
.room-dashboard {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.page-heading,
.panel-head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
}

.page-heading h2,
.panel-head h3 {
  margin: 4px 0 0;
  color: #1f3a5f;
  font-weight: 800;
}

.page-heading h2 {
  font-size: 1.7rem;
}

.panel-head h3 {
  font-size: 1.15rem;
}

.page-kicker {
  color: #1a73e8;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0;
  text-transform: uppercase;
}

.metric-sheet,
.tab-shell,
.panel,
.empty-state {
  border: 1px solid #e4e8ef;
  border-radius: 8px;
  background: #fff;
}

.metric-sheet {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 94px;
  padding: 18px;
}

.metric-sheet span {
  color: #607085;
  font-size: 0.88rem;
}

.metric-sheet strong {
  color: #1f3a5f;
  font-size: 1.8rem;
  line-height: 1;
}

.tab-shell {
  overflow: hidden;
}

.tab-content {
  min-height: 360px;
}

.panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  margin-top: 18px;
  padding: 18px;
}

.panel-toolbar {
  margin-bottom: 2px;
}

.room-tools {
  display: grid;
  grid-template-columns: minmax(240px, 360px) auto;
  align-items: center;
  gap: 12px;
}

.room-card {
  height: 100%;
  overflow: hidden;
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
  cursor: pointer;
}

.room-card:hover {
  transform: translateY(-4px);
}

.room-card-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.room-card-head small {
  display: block;
  margin-top: 3px;
  color: #607085;
}

.room-title {
  color: #1f3a5f;
  font-size: 1.1rem;
  font-weight: 800;
}

.room-meta {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 16px;
  color: #526174;
  font-size: 0.9rem;
}

.room-meta span {
  display: flex;
  align-items: center;
  gap: 8px;
}

.empty-state {
  padding: 28px;
  text-align: center;
  color: #607085;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
}

.data-table th,
.data-table td {
  border-bottom: 1px solid #e6ebf2;
  padding: 12px 10px;
  text-align: left;
  vertical-align: middle;
}

.data-table th {
  color: #526174;
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.table-empty {
  color: #607085;
  padding: 24px 10px;
  text-align: center;
}

.row-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.occupancy-cell,
.auto-status,
.building-status-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.occupancy-cell strong {
  color: #17365f;
  font-size: 0.98rem;
}

.occupancy-cell small,
.auto-status small,
.building-status-cell small {
  color: #607085;
  font-size: 0.76rem;
}

.building-detail-title {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.building-detail-title small {
  display: block;
  margin-top: 4px;
  color: #607085;
  font-size: 0.82rem;
  font-weight: 500;
}

.building-summary {
  display: grid;
  gap: 8px;
}

.building-summary p {
  margin: 0;
}

.compact-head {
  align-items: center;
  margin-bottom: 12px;
}

.notice-list {
  display: grid;
  gap: 12px;
}

.notice-item {
  border: 1px solid #e4e8ef;
  border-left: 4px solid #1a73e8;
  border-radius: 8px;
  padding: 14px;
}

.notice-critical {
  border-left-color: #d32f2f;
}

.notice-warning {
  border-left-color: #f9a825;
}

.notice-main,
.notice-title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.notice-title-row {
  align-items: center;
  justify-content: flex-start;
}

.notice-main small,
.notice-time {
  color: #607085;
}

.notice-item p {
  margin: 10px 0 6px;
  color: #34445a;
}

.room-dialog {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.room-dialog p {
  margin: 0;
}

.reference-row {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.reference-row small {
  color: #607085;
}

@media (max-width: 760px) {
  .page-heading,
  .panel-head {
    align-items: stretch;
    flex-direction: column;
  }

  .room-tools {
    grid-template-columns: 1fr;
  }

  .data-table {
    display: block;
    overflow-x: auto;
    white-space: nowrap;
  }
}
</style>

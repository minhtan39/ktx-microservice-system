<template>
  <v-dialog
    :model-value="modelValue"
    class="bed-mapper-dialog"
    max-width="1180"
    scrollable
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card class="bed-mapper-card">
      <v-card-title class="mapper-title">
        <div>
          <span class="page-kicker">Interactive Room & Bed Mapping</span>
          <strong>{{ roomTitle }}</strong>
          <small>{{ roomSubtitle }}</small>
        </div>
        <v-btn icon="mdi-close" variant="text" @click="emit('update:modelValue', false)" />
      </v-card-title>

      <v-card-text>
        <v-alert v-if="localError" type="error" variant="tonal" class="mb-4">
          {{ localError }}
        </v-alert>

        <section class="mapper-shell">
          <div class="room-map-panel">
            <div class="panel-headline">
              <div>
                <h3>Sơ đồ giường 2D</h3>
                <p>
                  Kéo sinh viên sang giường trống trong cùng phòng, hoặc chọn sinh viên rồi bấm
                  "Chuyển đến đây" trên thiết bị cảm ứng.
                </p>
              </div>
              <div class="room-capacity-pill">
                <strong>{{ occupiedCount }}/{{ capacity }}</strong>
                <span>giường đang dùng</span>
              </div>
            </div>

            <div class="bed-grid" :style="bedGridStyle">
              <button
                v-for="bed in beds"
                :key="bed.bedNumber"
                type="button"
                class="bed-tile"
                :class="{
                  occupied: bed.occupant,
                  empty: !bed.occupant,
                  focus: bed.isFocus,
                  selected: selectedStudentId && bed.occupant?.studentId === selectedStudentId,
                }"
                :draggable="Boolean(bed.occupant) && !readonly"
                @dragstart="startDrag(bed, $event)"
                @dragover.prevent
                @drop="dropOnBed(bed)"
                @click="selectBed(bed)"
              >
                <span class="bed-number">Giường {{ bed.bedNumber }}</span>
                <template v-if="bed.occupant">
                  <span class="bed-avatar">{{ initials(bed.occupant.student?.fullName) }}</span>
                  <strong>{{ bed.occupant.student?.fullName || `Sinh viên #${bed.occupant.studentId}` }}</strong>
                  <small>{{ bed.occupant.student?.studentCode || bed.occupant.contractCode }}</small>
                  <em v-if="bed.isFocus">Đang xem</em>
                </template>
                <template v-else>
                  <span class="empty-icon mdi mdi-bed-empty"></span>
                  <strong>Giường trống</strong>
                  <small>Sẵn sàng xếp sinh viên</small>
                  <span
                    v-if="selectedStudentId && !readonly"
                    class="move-target"
                    @click.stop="moveSelectedStudent(bed.bedNumber)"
                  >
                    Chuyển đến đây
                  </span>
                </template>
              </button>
            </div>
          </div>

          <div class="room-3d-panel">
            <div class="panel-headline compact">
              <div>
                <h3>Góc nhìn 3D</h3>
                <p>Giường xanh là còn trống, xanh đậm là đã có sinh viên, vàng là hồ sơ đang xem.</p>
              </div>
            </div>
            <canvas ref="threeCanvas" class="room-3d-canvas" aria-label="Sơ đồ phòng 3D"></canvas>
            <div class="legend-row">
              <span><i class="legend available"></i>Trống</span>
              <span><i class="legend occupied"></i>Đã xếp</span>
              <span><i class="legend focus"></i>Đang xem</span>
            </div>
          </div>
        </section>
      </v-card-text>

      <v-card-actions>
        <span class="mapper-note">
          Dữ liệu lấy từ RoomService occupancyReferences và đối chiếu tên sinh viên từ N2.
        </span>
        <v-spacer />
        <v-btn variant="text" @click="emit('update:modelValue', false)">Đóng</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import * as THREE from 'three'
import api from '@/services/api'

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  room: {
    type: Object,
    default: null,
  },
  students: {
    type: Array,
    default: () => [],
  },
  contracts: {
    type: Array,
    default: () => [],
  },
  focusStudentId: {
    type: [Number, String],
    default: null,
  },
  readonly: {
    type: Boolean,
    default: false,
  },
})

const emit = defineEmits(['update:modelValue', 'room-updated'])

const threeCanvas = ref(null)
const localError = ref('')
const selectedStudentId = ref(null)
const draggedStudentId = ref(null)
const moving = ref(false)

let renderer = null
let scene = null
let camera = null
let animationFrame = null
let roomGroup = null

const capacity = computed(() => Math.max(Number(props.room?.capacity || 0), 1))
const occupiedCount = computed(() => beds.value.filter((bed) => bed.occupant).length)
const studentMap = computed(() =>
  new Map(props.students.map((student) => [Number(student.id), student])),
)

const roomTitle = computed(() => {
  if (!props.room) return 'Chưa chọn phòng'

  return `Phòng ${props.room.roomNumber || props.room.roomId} - Tòa ${props.room.buildingName}`
})

const roomSubtitle = computed(() => {
  if (!props.room) return 'Chọn một phòng để xem sơ đồ giường.'

  return `${props.room.floorName || `Tầng ${props.room.floor || '-'}`} · ${props.room.roomType || 'Loại phòng'} · ${props.room.genderText || (props.room.gender ? 'Nam' : 'Nữ')}`
})

const bedGridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${capacity.value <= 4 ? 2 : 3}, minmax(0, 1fr))`,
}))

const roomOccupants = computed(() => {
  const references = Array.isArray(props.room?.occupancyReferences)
    ? props.room.occupancyReferences
    : []

  if (references.length > 0) {
    return references.map((reference, index) => ({
      studentId: Number(reference.studentId),
      registrationId: Number(reference.registrationId || 0),
      contractCode: reference.contractCode || '',
      occupiedAt: reference.occupiedAt,
      bedNumber: Number(reference.bedNumber || index + 1),
      student: studentMap.value.get(Number(reference.studentId)),
    }))
  }

  return props.contracts
    .filter((contract) =>
      Number(contract.roomId) === Number(props.room?.roomId) &&
      ['active', 'pendingsignature'].includes(String(contract.status || '').toLowerCase()))
    .map((contract, index) => ({
      studentId: Number(contract.studentId),
      registrationId: 0,
      contractCode: contract.contractCode || '',
      occupiedAt: contract.startDate,
      bedNumber: index + 1,
      student: studentMap.value.get(Number(contract.studentId)),
    }))
})

const beds = computed(() => {
  const usedBeds = new Set()
  const occupantByBed = new Map()

  roomOccupants.value.forEach((occupant) => {
    let bedNumber = Number(occupant.bedNumber || 0)

    if (bedNumber < 1 || bedNumber > capacity.value || usedBeds.has(bedNumber)) {
      bedNumber = Array.from({ length: capacity.value }, (_, index) => index + 1)
        .find((candidate) => !usedBeds.has(candidate)) || capacity.value
    }

    usedBeds.add(bedNumber)
    occupantByBed.set(bedNumber, { ...occupant, bedNumber })
  })

  return Array.from({ length: capacity.value }, (_, index) => {
    const bedNumber = index + 1
    const occupant = occupantByBed.get(bedNumber) || null
    const isFocus = Boolean(
      occupant &&
      props.focusStudentId &&
      Number(occupant.studentId) === Number(props.focusStudentId),
    )

    return {
      bedNumber,
      occupant,
      isFocus,
    }
  })
})

const initials = (name) => {
  return String(name || 'SV')
    .trim()
    .split(/\s+/)
    .slice(-2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

const selectBed = (bed) => {
  if (bed.occupant) {
    selectedStudentId.value = bed.occupant.studentId
  }
}

const startDrag = (bed, event) => {
  if (!bed.occupant || props.readonly) return

  selectedStudentId.value = bed.occupant.studentId
  draggedStudentId.value = bed.occupant.studentId
  event.dataTransfer.effectAllowed = 'move'
  event.dataTransfer.setData('text/plain', String(bed.occupant.studentId))
}

const dropOnBed = async (bed) => {
  const studentId = draggedStudentId.value || selectedStudentId.value
  draggedStudentId.value = null

  if (!studentId || bed.occupant || props.readonly) return

  await moveStudentToBed(studentId, bed.bedNumber)
}

const moveSelectedStudent = async (targetBedNumber) => {
  if (!selectedStudentId.value || props.readonly) return

  await moveStudentToBed(selectedStudentId.value, targetBedNumber)
}

const moveStudentToBed = async (studentId, targetBedNumber) => {
  if (!props.room?.roomId || moving.value) return

  try {
    moving.value = true
    localError.value = ''

    const response = await api.post(`/rooms/${props.room.roomId}/beds/move`, {
      studentId: Number(studentId),
      targetBedNumber,
    })

    selectedStudentId.value = Number(studentId)
    emit('room-updated', response.data)
  } catch (error) {
    localError.value = error.response?.data?.message || 'Không chuyển được giường. Kiểm tra giường đích còn trống và RoomService đang hoạt động.'
    console.error(error)
  } finally {
    moving.value = false
  }
}

const cleanupThree = () => {
  if (animationFrame) {
    cancelAnimationFrame(animationFrame)
    animationFrame = null
  }

  if (renderer) {
    renderer.dispose()
    renderer = null
  }

  scene = null
  camera = null
  roomGroup = null
}

const buildThreeScene = async () => {
  if (!props.modelValue) return

  await nextTick()

  const canvas = threeCanvas.value
  if (!canvas) return

  cleanupThree()

  const width = Math.max(canvas.clientWidth || 520, 320)
  const height = Math.max(canvas.clientHeight || 280, 240)

  renderer = new THREE.WebGLRenderer({
    canvas,
    antialias: true,
    alpha: true,
    powerPreference: 'high-performance',
    preserveDrawingBuffer: true,
  })
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2))
  renderer.setSize(width, height, false)
  renderer.setClearColor(0xf8fafc, 1)

  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(42, width / height, 0.1, 100)
  camera.position.set(4.8, 5.2, 7.2)
  camera.lookAt(0, 0, 0)

  scene.add(new THREE.AmbientLight(0xffffff, 0.72))

  const keyLight = new THREE.DirectionalLight(0xffffff, 0.86)
  keyLight.position.set(4, 7, 5)
  scene.add(keyLight)

  const floor = new THREE.Mesh(
    new THREE.BoxGeometry(7.2, 0.08, 4.8),
    new THREE.MeshStandardMaterial({ color: 0xe7edf3, roughness: 0.85 }),
  )
  floor.position.y = -0.08
  scene.add(floor)

  roomGroup = new THREE.Group()
  const columns = capacity.value <= 4 ? 2 : 3
  const spacingX = 2.1
  const spacingZ = 1.5
  const rows = Math.ceil(capacity.value / columns)
  const offsetX = ((columns - 1) * spacingX) / 2
  const offsetZ = ((rows - 1) * spacingZ) / 2

  beds.value.forEach((bed, index) => {
    const column = index % columns
    const row = Math.floor(index / columns)
    const color = bed.isFocus ? 0xf59e0b : bed.occupant ? 0x2563eb : 0x22c55e
    const bedMesh = new THREE.Mesh(
      new THREE.BoxGeometry(1.35, 0.32, 0.72),
      new THREE.MeshStandardMaterial({ color, roughness: 0.58, metalness: 0.04 }),
    )

    bedMesh.position.set(column * spacingX - offsetX, 0.18, row * spacingZ - offsetZ)
    roomGroup.add(bedMesh)

    const pillow = new THREE.Mesh(
      new THREE.BoxGeometry(0.34, 0.12, 0.62),
      new THREE.MeshStandardMaterial({ color: bed.occupant ? 0xdbeafe : 0xdcfce7, roughness: 0.72 }),
    )
    pillow.position.set(bedMesh.position.x - 0.42, 0.43, bedMesh.position.z)
    roomGroup.add(pillow)
  })

  scene.add(roomGroup)

  const animate = () => {
    if (!renderer || !scene || !camera) return

    if (roomGroup) {
      roomGroup.rotation.y = Math.sin(Date.now() * 0.0007) * 0.08
    }

    renderer.render(scene, camera)
    animationFrame = requestAnimationFrame(animate)
  }

  animate()
}

watch(
  () => [props.modelValue, props.room?.roomId, beds.value.map((bed) => `${bed.bedNumber}:${bed.occupant?.studentId || 0}:${bed.isFocus}`).join('|')],
  buildThreeScene,
  { immediate: true },
)

watch(
  () => props.modelValue,
  (visible) => {
    if (!visible) {
      localError.value = ''
      selectedStudentId.value = null
      draggedStudentId.value = null
      cleanupThree()
    }
  },
)

onBeforeUnmount(cleanupThree)
</script>

<style scoped>
.bed-mapper-card {
  border-radius: 14px;
}

.bed-mapper-dialog :deep(.v-overlay__content) {
  max-width: min(1180px, calc(100vw - 32px)) !important;
  width: min(1180px, calc(100vw - 32px)) !important;
}

.mapper-title {
  align-items: center;
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 20px 22px 12px;
}

.mapper-title div {
  display: grid;
  gap: 4px;
}

.mapper-title strong {
  color: #0f172a;
  font-size: 1.3rem;
}

.mapper-title small,
.panel-headline p,
.mapper-note {
  color: #64748b;
}

.mapper-shell {
  display: grid;
  grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
  gap: 18px;
}

.room-map-panel,
.room-3d-panel {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 18px;
}

.panel-headline {
  align-items: start;
  display: flex;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.panel-headline.compact {
  display: block;
}

.panel-headline h3 {
  color: #0f172a;
  font-size: 1.08rem;
  margin: 0 0 4px;
}

.panel-headline p {
  font-size: .9rem;
  line-height: 1.45;
  margin: 0;
}

.room-capacity-pill {
  background: #ffffff;
  border: 1px solid #dbeafe;
  border-radius: 10px;
  color: #1d4ed8;
  display: grid;
  min-width: 118px;
  padding: 10px 12px;
  text-align: right;
}

.room-capacity-pill strong {
  font-size: 1.25rem;
}

.room-capacity-pill span {
  color: #64748b;
  font-size: .78rem;
}

.bed-grid {
  display: grid;
  gap: 12px;
}

.bed-tile {
  align-items: start;
  background: #ffffff;
  border: 1px solid #dbeafe;
  border-radius: 12px;
  color: #0f172a;
  cursor: pointer;
  display: grid;
  gap: 7px;
  min-height: 144px;
  padding: 13px;
  text-align: left;
  transition: border-color .16s ease, box-shadow .16s ease, transform .16s ease;
}

.bed-tile:hover {
  border-color: #60a5fa;
  box-shadow: 0 12px 28px rgba(37, 99, 235, .12);
  transform: translateY(-1px);
}

.bed-tile.occupied {
  border-color: #93c5fd;
}

.bed-tile.empty {
  background: linear-gradient(135deg, #ffffff, #f0fdf4);
  border-color: #bbf7d0;
}

.bed-tile.focus {
  background: linear-gradient(135deg, #fff7ed, #ffffff);
  border-color: #f59e0b;
  box-shadow: 0 0 0 3px rgba(245, 158, 11, .16);
}

.bed-tile.selected {
  box-shadow: 0 0 0 3px rgba(37, 99, 235, .18);
}

.bed-number {
  color: #2563eb;
  font-size: .78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.bed-avatar {
  align-items: center;
  background: #dbeafe;
  border-radius: 50%;
  color: #1d4ed8;
  display: inline-flex;
  font-size: .78rem;
  font-weight: 900;
  height: 36px;
  justify-content: center;
  width: 36px;
}

.bed-tile strong {
  font-size: .95rem;
  line-height: 1.25;
}

.bed-tile small,
.bed-tile em {
  color: #64748b;
  font-size: .8rem;
  font-style: normal;
}

.bed-tile em {
  color: #b45309;
  font-weight: 800;
}

.empty-icon {
  color: #16a34a;
  font-size: 2rem;
}

.move-target {
  align-self: end;
  background: #dcfce7;
  border-radius: 999px;
  color: #15803d;
  display: inline-flex;
  font-size: .8rem;
  font-weight: 800;
  justify-content: center;
  padding: 7px 10px;
}

.room-3d-canvas {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  display: block;
  height: 300px;
  margin-top: 14px;
  width: 100%;
}

.legend-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 12px;
}

.legend-row span {
  align-items: center;
  color: #475569;
  display: inline-flex;
  font-size: .82rem;
  gap: 6px;
}

.legend {
  border-radius: 999px;
  display: inline-flex;
  height: 10px;
  width: 10px;
}

.legend.available {
  background: #22c55e;
}

.legend.occupied {
  background: #2563eb;
}

.legend.focus {
  background: #f59e0b;
}

.mapper-note {
  font-size: .84rem;
}

@media (max-width: 860px) {
  .mapper-shell {
    grid-template-columns: 1fr;
  }

  .panel-headline {
    display: grid;
  }
}

@media (max-width: 620px) {
  .mapper-title {
    padding: 16px;
  }

  .bed-grid {
    grid-template-columns: 1fr !important;
  }

  .room-map-panel,
  .room-3d-panel {
    padding: 14px;
  }

  .room-3d-canvas {
    height: 240px;
  }
}
</style>

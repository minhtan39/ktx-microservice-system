<template>
  <main class="landing-page">
    <nav :class="['landing-nav', { scrolled: navScrolled, open: mobileOpen }]">
      <a class="landing-brand" href="#hero" @click.prevent="scrollTo('hero')">
        <span class="mdi mdi-home-city-outline"></span>
        <strong>DormManager</strong>
      </a>

      <div class="landing-links">
        <a v-for="link in navLinks" :key="link.target" :href="`#${link.target}`" @click.prevent="scrollTo(link.target)">
          {{ link.label }}
        </a>
      </div>

      <div class="landing-actions">
        <router-link class="nav-login" to="/login">Đăng nhập</router-link>
        <button class="mobile-toggle" type="button" aria-label="Mở menu" @click="mobileOpen = !mobileOpen">
          <span :class="['mdi', mobileOpen ? 'mdi-close' : 'mdi-menu']"></span>
        </button>
      </div>

      <div v-if="mobileOpen" class="mobile-menu">
        <a v-for="link in navLinks" :key="`mobile-${link.target}`" :href="`#${link.target}`" @click.prevent="scrollTo(link.target)">
          {{ link.label }}
        </a>
        <router-link to="/login">Đăng nhập hệ thống</router-link>
      </div>
    </nav>

    <section id="hero" class="hero-section">
      <div class="hero-media" aria-hidden="true"></div>
      <div class="hero-overlay" aria-hidden="true"></div>

      <div class="landing-container hero-content">
        <span class="hero-badge">
          <i></i>
          Hệ thống quản lý ký túc xá sinh viên
        </span>

        <h1>
          Quản lý ký túc xá
          <span>{{ typedWord }}</span><b aria-hidden="true"></b>
        </h1>

        <p>
          Một cổng nội trú gọn gàng cho sinh viên đăng ký phòng, theo dõi hợp đồng,
          còn nhân viên có thể duyệt đơn, xếp phòng và phối hợp các dịch vụ trong cùng hệ thống.
        </p>

        <div class="hero-cta">
          <router-link class="primary-cta" to="/login">
            Bắt đầu ngay
            <span class="mdi mdi-arrow-right"></span>
          </router-link>
          <a class="secondary-cta" href="#preview" @click.prevent="scrollTo('preview')">
            Xem giao diện
          </a>
        </div>
      </div>

      <a class="scroll-cue" href="#stats" @click.prevent="scrollTo('stats')">
        <span>Khám phá thêm</span>
        <i></i>
      </a>
    </section>

    <section id="stats" ref="statsRef" class="stats-section">
      <div class="landing-container">
        <div class="section-heading compact">
          <span>Số liệu vận hành</span>
          <h2>Ký túc xá nhìn một lần là hiểu</h2>
        </div>

        <div class="stats-grid">
          <article v-for="item in displayStats" :key="item.label" class="stat-card">
            <div class="stat-icon">
              <span :class="['mdi', item.icon]"></span>
            </div>
            <strong>{{ item.value }}{{ item.suffix }}</strong>
            <p>{{ item.label }}</p>
          </article>
        </div>
      </div>
    </section>

    <section id="features" class="features-section">
      <div class="landing-container">
        <div class="section-heading">
          <span>Tính năng đúng đề tài</span>
          <h2>Tập trung vào nghiệp vụ ký túc xá, không lan man</h2>
          <p>
            Giao diện được sắp lại theo đúng luồng thầy chấm: hồ sơ sinh viên,
            đăng ký phòng online, duyệt xếp phòng, hợp đồng và liên thông dịch vụ.
          </p>
        </div>

        <div class="feature-grid">
          <article v-for="feature in features" :key="feature.title" class="feature-card">
            <div :class="['feature-icon', feature.tone]">
              <span :class="['mdi', feature.icon]"></span>
            </div>
            <h3>{{ feature.title }}</h3>
            <p>{{ feature.text }}</p>
          </article>
        </div>
      </div>
    </section>

    <section id="workflow" class="workflow-section">
      <div class="landing-container">
        <div class="section-heading">
          <span>Quy trình nội trú</span>
          <h2>Từ đăng ký đến nhận phòng trong một mạch rõ ràng</h2>
        </div>

        <div class="workflow-track" aria-hidden="true">
          <i :style="{ width: workflowProgress }"></i>
        </div>

        <div class="workflow-grid">
          <article v-for="step in workflow" :key="step.index" class="workflow-card">
            <div class="step-number">{{ step.index }}</div>
            <div class="step-icon">
              <span :class="['mdi', step.icon]"></span>
            </div>
            <h3>{{ step.title }}</h3>
            <p>{{ step.text }}</p>
          </article>
        </div>
      </div>
    </section>

    <section id="preview" class="preview-section">
      <div class="landing-container preview-layout">
        <div class="preview-copy">
          <span class="section-pill">Tổng quan hệ thống</span>
          <h2>Màn hình quản trị được làm cho người vận hành thật sự dùng</h2>
          <p>
            Admin và nhân viên thấy nhanh đơn cần duyệt, giường còn trống, hợp đồng hiệu lực
            và trạng thái kết nối giữa Room, Contract & Student, Billing qua Gateway.
          </p>
          <div class="preview-points">
            <span><i class="mdi mdi-check-circle-outline"></i> Dữ liệu phòng theo RoomService</span>
            <span><i class="mdi mdi-check-circle-outline"></i> Hợp đồng sinh tự động sau khi duyệt</span>
            <span><i class="mdi mdi-check-circle-outline"></i> Sinh viên có cổng theo dõi riêng</span>
          </div>
        </div>

        <div class="dashboard-mockup" aria-label="Bản xem trước dashboard DormManager">
          <div class="mock-sidebar">
            <span></span>
            <b></b>
            <i v-for="n in 7" :key="n"></i>
          </div>
          <div class="mock-main">
            <header>
              <div>
                <span></span>
                <strong></strong>
              </div>
              <b></b>
            </header>
            <div class="mock-hero">
              <div>
                <span></span>
                <strong></strong>
                <i></i>
              </div>
              <div class="mock-mini"></div>
            </div>
            <div class="mock-cards">
              <article v-for="card in mockCards" :key="card.label">
                <span :class="card.tone"></span>
                <strong>{{ card.value }}</strong>
                <p>{{ card.label }}</p>
              </article>
            </div>
            <div class="mock-table">
              <span v-for="n in 5" :key="n"></span>
            </div>
          </div>
          <div class="floating-badge realtime"><i></i>Dữ liệu thời gian thực</div>
          <div class="floating-badge roles"><span class="mdi mdi-shield-check-outline"></span>Phân quyền 3 vai trò</div>
        </div>
      </div>
    </section>

    <section id="contact" class="landing-cta">
      <div class="cta-media" aria-hidden="true"></div>
      <div class="cta-overlay" aria-hidden="true"></div>
      <div class="landing-container cta-content">
        <h2>Vào DormManager để tiếp tục nghiệp vụ nội trú</h2>
        <p>Sinh viên đăng ký và theo dõi hồ sơ. Nhân viên xử lý đơn, xếp phòng, hợp đồng và liên thông dịch vụ.</p>
        <div class="hero-cta">
          <router-link class="white-cta" to="/login">
            <span class="mdi mdi-login"></span>
            Đăng nhập hệ thống
          </router-link>
          <a class="outline-cta" href="#hero" @click.prevent="scrollTo('hero')">
            <span class="mdi mdi-arrow-up"></span>
            Về đầu trang
          </a>
        </div>
      </div>
    </section>

    <footer class="landing-footer">
      <div class="landing-container footer-grid">
        <div>
          <a class="footer-brand" href="#hero" @click.prevent="scrollTo('hero')">
            <span class="mdi mdi-home-city-outline"></span>
            <strong>DormManager</strong>
          </a>
          <p>Hệ thống quản lý ký túc xá sinh viên theo kiến trúc microservice.</p>
        </div>
        <div>
          <h3>Tính năng</h3>
          <a href="#features" @click.prevent="scrollTo('features')">Hồ sơ sinh viên</a>
          <a href="#features" @click.prevent="scrollTo('features')">Đăng ký nội trú</a>
          <a href="#features" @click.prevent="scrollTo('features')">Hợp đồng phòng</a>
        </div>
        <div>
          <h3>Dịch vụ</h3>
          <a href="#workflow" @click.prevent="scrollTo('workflow')">Room & Building</a>
          <a href="#workflow" @click.prevent="scrollTo('workflow')">Contract & Student</a>
          <a href="#workflow" @click.prevent="scrollTo('workflow')">Billing & Maintenance</a>
        </div>
        <div>
          <h3>Liên hệ</h3>
          <p>Khoa Công nghệ thông tin</p>
          <p>dormmanager@school.local</p>
          <p>Hotline nội trú: 1900 2026</p>
        </div>
      </div>
      <div class="footer-bottom">
        <span>© 2026 DormManager. Student Dormitory Microservice System.</span>
      </div>
    </footer>
  </main>
</template>

<script setup>
import { onMounted, onUnmounted, ref } from 'vue'

const navScrolled = ref(false)
const mobileOpen = ref(false)
const typedWord = ref('')
const statsRef = ref(null)
const workflowProgress = ref('0%')

const navLinks = [
  { label: 'Tính năng', target: 'features' },
  { label: 'Quy trình', target: 'workflow' },
  { label: 'Tổng quan', target: 'preview' },
  { label: 'Liên hệ', target: 'contact' },
]

const words = ['thông minh', 'hiệu quả', 'minh bạch', 'hiện đại']
let typeTimer = null
let wordIndex = 0
let charIndex = 0
let deleting = false

const stats = [
  { icon: 'mdi-account-star-outline', target: 486, suffix: '+', label: 'Sinh viên nội trú' },
  { icon: 'mdi-office-building-outline', target: 4, suffix: '', label: 'Tòa nhà hiện đại' },
  { icon: 'mdi-emoticon-happy-outline', target: 98, suffix: '%', label: 'Tỉ lệ hài lòng' },
  { icon: 'mdi-bed-outline', target: 34, suffix: '', label: 'Giường còn trống' },
]

const displayStats = ref(stats.map((item) => ({ ...item, value: 0 })))

const features = [
  {
    icon: 'mdi-account-school-outline',
    tone: 'green',
    title: 'Quản lý hồ sơ sinh viên',
    text: 'Lưu thông tin cá nhân, lớp, khoa, liên hệ và lịch sử lưu trú theo đúng endpoint /students.',
  },
  {
    icon: 'mdi-file-document-edit-outline',
    tone: 'amber',
    title: 'Đăng ký phòng trực tuyến',
    text: 'Sinh viên gửi nguyện vọng phòng, hệ thống ghi nhận ưu tiên và thời gian ở tại /registrations.',
  },
  {
    icon: 'mdi-clipboard-check-multiple-outline',
    tone: 'green',
    title: 'Duyệt xếp phòng thông minh',
    text: 'Nhân viên đối chiếu giường trống từ RoomService, có thể chọn tòa và phòng phù hợp khi phòng mong muốn đã đầy.',
  },
  {
    icon: 'mdi-file-sign',
    tone: 'amber',
    title: 'Tạo hợp đồng thuê phòng',
    text: 'Tự tạo hợp đồng, thời hạn ở, điều khoản, tiền cọc và tiền phòng sau khi đơn được duyệt tại /contracts.',
  },
  {
    icon: 'mdi-view-grid-outline',
    tone: 'slate',
    title: 'Sơ đồ phòng trực quan',
    text: 'Theo dõi tòa, phòng, sức chứa, giới tính phòng và số giường còn trống từ nhóm Room & Building.',
  },
  {
    icon: 'mdi-tools',
    tone: 'amber',
    title: 'Sự cố và bảo trì',
    text: 'Liên thông Billing & Maintenance để theo dõi sự cố phòng ở và các khoản thu phát sinh từ hợp đồng.',
  },
]

const workflow = [
  {
    index: '01',
    icon: 'mdi-account-plus-outline',
    title: 'Tạo tài khoản sinh viên',
    text: 'Admin tạo hồ sơ, tài khoản mặc định dùng mã sinh viên để sinh viên đăng nhập lần đầu.',
  },
  {
    index: '02',
    icon: 'mdi-form-select',
    title: 'Nộp đơn nội trú',
    text: 'Sinh viên chọn nguyện vọng tòa, loại phòng, thời hạn ở và thông tin ưu tiên.',
  },
  {
    index: '03',
    icon: 'mdi-home-search-outline',
    title: 'Duyệt và xếp phòng',
    text: 'Nhân viên kiểm tra phòng trống, chấp nhận nguyện vọng hoặc chọn phòng thay thế phù hợp.',
  },
  {
    index: '04',
    icon: 'mdi-file-check-outline',
    title: 'Ký hợp đồng',
    text: 'Hệ thống sinh hợp đồng và gửi khoản thu sang BillingService để hoàn tất luồng liên thông.',
  },
]

const mockCards = [
  { value: '486', label: 'Sinh viên', tone: 'green' },
  { value: '12', label: 'Đơn chờ', tone: 'amber' },
  { value: '34', label: 'Giường trống', tone: 'green' },
]

const scrollTo = (target) => {
  document.getElementById(target)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  mobileOpen.value = false
}

const handleScroll = () => {
  navScrolled.value = window.scrollY > 60
}

const runTypewriter = () => {
  const currentWord = words[wordIndex]

  if (!deleting) {
    charIndex += 1
    typedWord.value = currentWord.slice(0, charIndex)

    if (charIndex === currentWord.length) {
      deleting = true
      typeTimer = window.setTimeout(runTypewriter, 1600)
      return
    }
  } else {
    charIndex -= 1
    typedWord.value = currentWord.slice(0, charIndex)

    if (charIndex === 0) {
      deleting = false
      wordIndex = (wordIndex + 1) % words.length
    }
  }

  typeTimer = window.setTimeout(runTypewriter, deleting ? 42 : 78)
}

const animateCounters = () => {
  const duration = 1500
  const start = performance.now()

  const frame = (now) => {
    const progress = Math.min((now - start) / duration, 1)
    const eased = 1 - Math.pow(1 - progress, 3)

    displayStats.value = stats.map((item) => ({
      ...item,
      value: Math.round(item.target * eased),
    }))

    if (progress < 1) requestAnimationFrame(frame)
  }

  requestAnimationFrame(frame)
}

onMounted(() => {
  handleScroll()
  window.addEventListener('scroll', handleScroll, { passive: true })
  runTypewriter()

  const observer = new IntersectionObserver(
    ([entry]) => {
      if (entry.isIntersecting) {
        animateCounters()
        workflowProgress.value = '100%'
        observer.disconnect()
      }
    },
    { threshold: 0.25 },
  )

  if (statsRef.value) observer.observe(statsRef.value)
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  if (typeTimer) window.clearTimeout(typeTimer)
})
</script>

<style scoped>
.landing-page {
  min-height: 100vh;
  background: var(--background-50);
  color: var(--foreground-950);
  font-family: var(--font-body);
}

.landing-container {
  width: min(1180px, calc(100% - 40px));
  margin: 0 auto;
}

.landing-nav {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 50;
  display: grid;
  grid-template-columns: minmax(210px, 1fr) auto minmax(210px, 1fr);
  align-items: center;
  gap: 24px;
  min-height: 78px;
  padding: 0 34px;
  color: #ffffff;
  transition: background-color 0.25s ease, border-color 0.25s ease, color 0.25s ease;
}

.landing-nav.scrolled,
.landing-nav.open {
  border-bottom: 1px solid rgba(226, 232, 240, 0.9);
  background: rgba(255, 255, 255, 0.96);
  color: var(--foreground-950);
  backdrop-filter: blur(16px);
}

.landing-brand,
.footer-brand {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: inherit;
  text-decoration: none;
}

.landing-brand .mdi,
.footer-brand .mdi {
  color: var(--primary-500);
  font-size: 32px;
}

.landing-brand strong,
.footer-brand strong {
  font-family: var(--font-heading);
  font-size: 22px;
  font-weight: 900;
}

.landing-links {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  justify-self: center;
  min-height: 44px;
  padding: 5px;
  border: 1px solid rgba(255, 255, 255, 0.16);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.08);
}

.landing-nav.scrolled .landing-links,
.landing-nav.open .landing-links {
  border-color: var(--background-200);
  background: #ffffff;
}

.landing-links a,
.mobile-menu a {
  min-height: 34px;
  padding: 8px 14px;
  border-radius: 999px;
  color: inherit;
  font-size: 14px;
  font-weight: 800;
  text-decoration: none;
}

.landing-links a:hover {
  background: rgba(255, 255, 255, 0.14);
}

.landing-nav.scrolled .landing-links a:hover,
.landing-nav.open .landing-links a:hover {
  background: var(--primary-50);
  color: var(--primary-700);
}

.landing-actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
}

.nav-login,
.primary-cta,
.secondary-cta,
.white-cta,
.outline-cta {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 9px;
  min-height: 44px;
  border-radius: 999px;
  font-weight: 900;
  text-decoration: none;
  transition: transform 0.2s ease, background-color 0.2s ease, border-color 0.2s ease;
}

.nav-login {
  padding: 0 18px;
  border: 1px solid rgba(255, 255, 255, 0.22);
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
}

.landing-nav.scrolled .nav-login,
.landing-nav.open .nav-login {
  border-color: transparent;
  background: var(--primary-500);
  color: #ffffff;
}

.nav-login:hover,
.primary-cta:hover,
.secondary-cta:hover,
.white-cta:hover,
.outline-cta:hover {
  transform: translateY(-2px);
}

.mobile-toggle {
  display: none;
  place-items: center;
  width: 44px;
  height: 44px;
  border: 1px solid rgba(255, 255, 255, 0.22);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.12);
  color: inherit;
  cursor: pointer;
}

.landing-nav.scrolled .mobile-toggle,
.landing-nav.open .mobile-toggle {
  border-color: var(--background-200);
  background: #ffffff;
}

.mobile-toggle .mdi {
  font-size: 24px;
}

.mobile-menu {
  position: absolute;
  top: 78px;
  left: 18px;
  right: 18px;
  display: grid;
  gap: 6px;
  padding: 14px;
  border: 1px solid var(--background-200);
  border-radius: 16px;
  background: #ffffff;
  color: var(--foreground-950);
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.13);
}

.mobile-menu a:hover {
  background: var(--primary-50);
  color: var(--primary-700);
}

.hero-section {
  position: relative;
  display: grid;
  place-items: center;
  min-height: 100vh;
  overflow: hidden;
  color: #ffffff;
}

.hero-media,
.hero-overlay,
.cta-media,
.cta-overlay {
  position: absolute;
  inset: 0;
}

.hero-media {
  background:
    linear-gradient(90deg, rgba(15, 23, 42, 0.2), transparent 44%),
    url("https://images.unsplash.com/photo-1562774053-701939374585?auto=format&fit=crop&w=1800&q=82") center/cover;
  transform: scale(1.02);
}

.hero-overlay {
  background:
    linear-gradient(180deg, rgba(2, 6, 23, 0.58), rgba(2, 6, 23, 0.32) 42%, rgba(2, 6, 23, 0.72)),
    linear-gradient(90deg, rgba(2, 44, 34, 0.76), rgba(2, 44, 34, 0.12));
}

.hero-content {
  position: relative;
  z-index: 2;
  padding: 130px 0 92px;
}

.hero-badge,
.section-pill,
.section-heading span {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 34px;
  padding: 0 14px;
  border-radius: 999px;
  font-size: 13px;
  font-weight: 900;
}

.hero-badge {
  border: 1px solid rgba(255, 255, 255, 0.18);
  background: rgba(255, 255, 255, 0.12);
  backdrop-filter: blur(10px);
}

.hero-badge i,
.floating-badge i {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: #3ef0a4;
  animation: pulse-dot 1.4s infinite;
}

.hero-content h1 {
  max-width: 920px;
  margin: 20px 0 0;
  font-family: var(--font-heading);
  font-size: clamp(46px, 7vw, 92px);
  font-weight: 900;
  line-height: 1.02;
}

.hero-content h1 span {
  color: #78f2bd;
}

.hero-content h1 b {
  display: inline-block;
  width: 4px;
  height: 0.78em;
  margin-left: 8px;
  border-radius: 999px;
  background: #78f2bd;
  vertical-align: -0.08em;
  animation: cursor-blink 0.85s infinite;
}

.hero-content p {
  max-width: 710px;
  margin: 22px 0 0;
  color: rgba(255, 255, 255, 0.78);
  font-size: clamp(17px, 2vw, 21px);
  line-height: 1.7;
}

.hero-cta {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
  margin-top: 34px;
}

.primary-cta {
  min-height: 52px;
  padding: 0 24px;
  background: var(--primary-500);
  color: #ffffff;
  box-shadow: 0 16px 30px rgba(19, 157, 113, 0.32);
}

.primary-cta .mdi {
  transition: transform 0.2s ease;
}

.primary-cta:hover .mdi {
  transform: translateX(4px);
}

.secondary-cta {
  min-height: 52px;
  padding: 0 24px;
  border: 1px solid rgba(255, 255, 255, 0.26);
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
  backdrop-filter: blur(10px);
}

.scroll-cue {
  position: absolute;
  left: 50%;
  bottom: 26px;
  z-index: 2;
  display: grid;
  justify-items: center;
  gap: 8px;
  color: rgba(255, 255, 255, 0.74);
  font-size: 13px;
  font-weight: 800;
  text-decoration: none;
  transform: translateX(-50%);
}

.scroll-cue i {
  width: 23px;
  height: 38px;
  border: 2px solid rgba(255, 255, 255, 0.5);
  border-radius: 999px;
}

.scroll-cue i::before {
  content: "";
  display: block;
  width: 4px;
  height: 8px;
  margin: 8px auto;
  border-radius: 999px;
  background: #ffffff;
  animation: wheel 1.4s infinite;
}

.stats-section,
.features-section,
.workflow-section,
.preview-section {
  padding: 92px 0;
}

.stats-section,
.workflow-section {
  background: var(--background-50);
}

.features-section,
.preview-section {
  background: var(--background-100);
}

.section-heading {
  max-width: 760px;
  margin-bottom: 38px;
}

.section-heading.compact {
  text-align: center;
  margin: 0 auto 38px;
}

.section-heading span,
.section-pill {
  background: var(--primary-50);
  color: var(--primary-700);
}

.section-heading h2,
.preview-copy h2,
.cta-content h2 {
  margin: 14px 0 0;
  font-family: var(--font-heading);
  font-size: clamp(31px, 4vw, 48px);
  line-height: 1.12;
  letter-spacing: 0;
}

.section-heading p,
.preview-copy p,
.cta-content p {
  margin: 16px 0 0;
  color: var(--foreground-600);
  font-size: 17px;
  line-height: 1.75;
}

.stats-grid,
.feature-grid,
.workflow-grid {
  display: grid;
  gap: 18px;
}

.stats-grid {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.stat-card,
.feature-card,
.workflow-card {
  border: 1px solid var(--background-200);
  border-radius: 18px;
  background: #ffffff;
}

.stat-card {
  min-height: 186px;
  padding: 24px;
  transition: transform 0.2s ease, border-color 0.2s ease;
}

.stat-card:hover,
.feature-card:hover,
.workflow-card:hover {
  transform: translateY(-4px);
  border-color: rgba(20, 157, 113, 0.32);
}

.stat-icon,
.feature-icon,
.step-icon {
  display: grid;
  place-items: center;
  width: 52px;
  height: 52px;
  border-radius: 14px;
}

.stat-icon,
.feature-icon.green,
.step-icon {
  background: var(--primary-50);
  color: var(--primary-700);
}

.feature-icon.amber {
  background: var(--accent-50);
  color: var(--accent-700);
}

.feature-icon.slate {
  background: var(--secondary-100);
  color: var(--secondary-700);
}

.stat-icon .mdi,
.feature-icon .mdi,
.step-icon .mdi {
  font-size: 29px;
}

.stat-card strong {
  display: block;
  margin-top: 22px;
  color: var(--foreground-950);
  font-family: var(--font-heading);
  font-size: 43px;
  line-height: 1;
}

.stat-card p {
  margin: 10px 0 0;
  color: var(--foreground-600);
  font-weight: 800;
}

.feature-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.feature-card {
  min-height: 260px;
  padding: 26px;
  transition: transform 0.2s ease, border-color 0.2s ease;
}

.feature-card h3,
.workflow-card h3 {
  margin: 22px 0 0;
  color: var(--foreground-950);
  font-family: var(--font-heading);
  font-size: 21px;
  line-height: 1.3;
}

.feature-card p,
.workflow-card p {
  margin: 12px 0 0;
  color: var(--foreground-600);
  line-height: 1.65;
}

.workflow-section .landing-container {
  position: relative;
}

.workflow-track {
  position: absolute;
  left: calc((100% - min(1180px, calc(100% - 40px))) / 2 + 70px);
  right: calc((100% - min(1180px, calc(100% - 40px))) / 2 + 70px);
  top: 238px;
  height: 3px;
  background: var(--background-200);
}

.workflow-track i {
  display: block;
  width: 0;
  height: 100%;
  border-radius: 999px;
  background: var(--primary-400);
  transition: width 1.7s cubic-bezier(0.2, 0.8, 0.2, 1);
}

.workflow-grid {
  position: relative;
  z-index: 1;
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.workflow-card {
  min-height: 282px;
  padding: 24px;
}

.step-number {
  display: grid;
  place-items: center;
  width: 58px;
  height: 58px;
  border: 2px solid var(--primary-200);
  border-radius: 999px;
  background: #ffffff;
  color: var(--primary-700);
  font-family: var(--font-heading);
  font-size: 20px;
  font-weight: 900;
}

.step-icon {
  margin-top: 24px;
}

.preview-layout {
  display: grid;
  grid-template-columns: minmax(320px, 0.82fr) minmax(0, 1.18fr);
  gap: 48px;
  align-items: center;
}

.preview-points {
  display: grid;
  gap: 12px;
  margin-top: 26px;
}

.preview-points span {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--foreground-800);
  font-weight: 800;
}

.preview-points .mdi {
  color: var(--primary-600);
  font-size: 22px;
}

.dashboard-mockup {
  position: relative;
  display: grid;
  grid-template-columns: 154px minmax(0, 1fr);
  min-height: 462px;
  overflow: hidden;
  border: 1px solid var(--background-200);
  border-radius: 24px;
  background: #ffffff;
  box-shadow: 0 28px 70px rgba(15, 23, 42, 0.11);
}

.mock-sidebar {
  padding: 22px 16px;
  background: #050606;
}

.mock-sidebar span {
  display: block;
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: var(--primary-500);
}

.mock-sidebar b {
  display: block;
  width: 100%;
  height: 22px;
  margin: 18px 0 24px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.16);
}

.mock-sidebar i {
  display: block;
  width: 100%;
  height: 31px;
  margin: 10px 0;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.08);
}

.mock-sidebar i:nth-child(4) {
  background: rgba(20, 157, 113, 0.28);
}

.mock-main {
  padding: 22px;
  background: var(--background-50);
}

.mock-main header {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  min-height: 56px;
}

.mock-main header span,
.mock-main header strong,
.mock-main header b,
.mock-hero span,
.mock-hero strong,
.mock-hero i,
.mock-table span {
  display: block;
  border-radius: 999px;
  background: var(--background-200);
}

.mock-main header span {
  width: 130px;
  height: 12px;
}

.mock-main header strong {
  width: 240px;
  height: 19px;
  margin-top: 9px;
}

.mock-main header b {
  width: 84px;
  height: 38px;
  background: var(--primary-100);
}

.mock-hero {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 170px;
  gap: 18px;
  min-height: 140px;
  padding: 20px;
  border-radius: 16px;
  background: linear-gradient(135deg, #ffffff, #effaf4);
  border: 1px solid var(--background-200);
}

.mock-hero span {
  width: 112px;
  height: 13px;
  background: var(--primary-100);
}

.mock-hero strong {
  width: 75%;
  height: 34px;
  margin-top: 22px;
  background: var(--foreground-950);
}

.mock-hero i {
  width: 52%;
  height: 15px;
  margin-top: 14px;
}

.mock-mini {
  border-radius: 16px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.15), transparent),
    var(--primary-500);
}

.mock-cards {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-top: 14px;
}

.mock-cards article {
  min-height: 112px;
  padding: 16px;
  border: 1px solid var(--background-200);
  border-radius: 14px;
  background: #ffffff;
}

.mock-cards span {
  display: block;
  width: 32px;
  height: 32px;
  border-radius: 10px;
}

.mock-cards span.green {
  background: var(--primary-100);
}

.mock-cards span.amber {
  background: var(--accent-100);
}

.mock-cards strong {
  display: block;
  margin-top: 14px;
  color: var(--foreground-950);
  font-size: 25px;
}

.mock-cards p {
  margin: 2px 0 0;
  color: var(--foreground-500);
  font-size: 13px;
  font-weight: 800;
}

.mock-table {
  display: grid;
  gap: 9px;
  margin-top: 14px;
  padding: 16px;
  border: 1px solid var(--background-200);
  border-radius: 14px;
  background: #ffffff;
}

.mock-table span {
  height: 13px;
}

.mock-table span:nth-child(odd) {
  width: 86%;
}

.floating-badge {
  position: absolute;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 40px;
  padding: 0 14px;
  border: 1px solid var(--background-200);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.94);
  color: var(--foreground-900);
  font-size: 13px;
  font-weight: 900;
  box-shadow: 0 14px 30px rgba(15, 23, 42, 0.1);
}

.floating-badge.realtime {
  top: 24px;
  right: 24px;
}

.floating-badge.roles {
  left: 24px;
  bottom: 24px;
}

.floating-badge .mdi {
  color: var(--primary-600);
  font-size: 20px;
}

.landing-cta {
  position: relative;
  min-height: 420px;
  display: grid;
  place-items: center;
  overflow: hidden;
  color: #ffffff;
}

.cta-media {
  background: url("https://images.unsplash.com/photo-1523050854058-8df90110c9f1?auto=format&fit=crop&w=1700&q=82") center/cover;
}

.cta-overlay {
  background: linear-gradient(90deg, rgba(3, 35, 29, 0.86), rgba(3, 35, 29, 0.46));
}

.cta-content {
  position: relative;
  z-index: 1;
}

.cta-content h2 {
  max-width: 820px;
  color: #ffffff;
}

.cta-content p {
  max-width: 650px;
  color: rgba(255, 255, 255, 0.74);
}

.white-cta,
.outline-cta {
  min-height: 52px;
  padding: 0 22px;
}

.white-cta {
  background: #ffffff;
  color: var(--primary-700);
}

.outline-cta {
  border: 1px solid rgba(255, 255, 255, 0.35);
  background: rgba(255, 255, 255, 0.12);
  color: #ffffff;
  backdrop-filter: blur(10px);
}

.landing-footer {
  background: #060808;
  color: rgba(255, 255, 255, 0.64);
}

.footer-grid {
  display: grid;
  grid-template-columns: 1.4fr repeat(3, 1fr);
  gap: 32px;
  padding: 58px 0;
}

.footer-brand {
  color: #ffffff;
}

.footer-grid h3 {
  margin: 0 0 16px;
  color: #ffffff;
  font-family: var(--font-heading);
  font-size: 16px;
}

.footer-grid p,
.footer-grid a {
  display: block;
  margin: 0 0 10px;
  color: rgba(255, 255, 255, 0.64);
  line-height: 1.65;
  text-decoration: none;
}

.footer-grid a:hover {
  color: #ffffff;
}

.footer-bottom {
  min-height: 58px;
  display: grid;
  place-items: center;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.45);
  font-size: 13px;
}

@keyframes cursor-blink {
  0%,
  45% {
    opacity: 1;
  }
  46%,
  100% {
    opacity: 0;
  }
}

@keyframes pulse-dot {
  0%,
  100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.6);
    opacity: 0.45;
  }
}

@keyframes wheel {
  0% {
    transform: translateY(0);
    opacity: 1;
  }
  100% {
    transform: translateY(12px);
    opacity: 0;
  }
}

@media (max-width: 1080px) {
  .landing-nav {
    grid-template-columns: 1fr auto;
  }

  .landing-links {
    display: none;
  }

  .mobile-toggle {
    display: grid;
  }

  .stats-grid,
  .feature-grid,
  .workflow-grid,
  .preview-layout,
  .footer-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .workflow-track {
    display: none;
  }
}

@media (max-width: 760px) {
  .landing-container {
    width: min(100% - 28px, 1180px);
  }

  .landing-nav {
    min-height: 70px;
    padding: 0 16px;
  }

  .landing-brand strong {
    font-size: 19px;
  }

  .nav-login {
    display: none;
  }

  .mobile-menu {
    top: 70px;
  }

  .hero-content {
    padding: 116px 0 82px;
  }

  .hero-content h1 {
    font-size: clamp(40px, 13vw, 58px);
  }

  .stats-section,
  .features-section,
  .workflow-section,
  .preview-section {
    padding: 66px 0;
  }

  .stats-grid,
  .feature-grid,
  .workflow-grid,
  .preview-layout,
  .footer-grid,
  .dashboard-mockup,
  .mock-hero,
  .mock-cards {
    grid-template-columns: 1fr;
  }

  .dashboard-mockup {
    min-height: auto;
  }

  .mock-sidebar {
    display: none;
  }

  .floating-badge {
    position: static;
    margin: 12px 14px 0;
    justify-content: center;
  }

  .landing-cta {
    min-height: 480px;
  }
}
</style>

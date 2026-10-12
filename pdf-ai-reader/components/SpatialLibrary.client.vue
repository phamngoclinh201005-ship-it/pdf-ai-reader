<script setup lang="ts">
import * as THREE from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { VRButton } from 'three/addons/webxr/VRButton.js'
import type { LibraryItem } from '~/types/library'
const props = defineProps<{ items: LibraryItem[] }>()
const emit = defineEmits<{ select: [id: string] }>()
const host = ref<HTMLDivElement | null>(null)
const xrHost = ref<HTMLDivElement | null>(null)
const error = ref('')
const panoramaName = ref('')
const vrAvailable = ref(false)
let renderer: THREE.WebGLRenderer | undefined
let scene: THREE.Scene | undefined
let camera: THREE.PerspectiveCamera | undefined
let controls: OrbitControls | undefined
let observer: ResizeObserver | undefined
let disposed = false
let panorama: THREE.Texture | undefined
let books: THREE.Mesh[] = []
const raycaster = new THREE.Raycaster()
let pointerStart = { x: 0, y: 0 }
function pick(event: PointerEvent) {
  if (!renderer || !camera || Math.hypot(event.clientX - pointerStart.x, event.clientY - pointerStart.y) > 6) return
  const rect = renderer.domElement.getBoundingClientRect()
  raycaster.setFromCamera(new THREE.Vector2((event.clientX - rect.left) / rect.width * 2 - 1, -(event.clientY - rect.top) / rect.height * 2 + 1), camera)
  const hit = raycaster.intersectObjects(books)[0]
  if (hit?.object.userData.id) emit('select', hit.object.userData.id)
}
function pointerDown(event: PointerEvent) { pointerStart = { x: event.clientX, y: event.clientY } }
function free(object: THREE.Object3D) {
  object.traverse(node => {
    if (node instanceof THREE.Mesh || node instanceof THREE.Points) {
      node.geometry.dispose()
      const materials = Array.isArray(node.material) ? node.material : [node.material]
      for (const material of materials) {
        if ('map' in material && material.map instanceof THREE.Texture) material.map.dispose()
        material.dispose()
      }
    }
  })
}
function buildBooks() {
  if (!scene) return
  for (const book of books) { scene.remove(book); free(book) }
  books = []
  const items = props.items.slice(0, 24)
  for (let i = 0; i < items.length; i++) {
    const item = items[i]!
    const angle = (i % 8) / 8 * Math.PI * 2
    const tier = Math.floor(i / 8)
    const canvas = document.createElement('canvas'); canvas.width = 512; canvas.height = 640
    const context = canvas.getContext('2d')!
    const gradient = context.createLinearGradient(0, 0, 512, 640)
    gradient.addColorStop(0, ['#7c3aed', '#2563eb', '#0891b2'][tier]!); gradient.addColorStop(1, '#161329')
    context.fillStyle = gradient; context.fillRect(0, 0, 512, 640)
    context.strokeStyle = '#c4b5fd'; context.lineWidth = 4; context.strokeRect(22, 22, 468, 596)
    context.fillStyle = '#ffffff'; context.font = 'bold 38px sans-serif'; context.fillText('PaperFlow', 44, 90)
    context.font = '28px sans-serif'
    const name = item.name.replace(/\.pdf$/i, '')
    for (let row = 0; row < 5; row++) context.fillText(name.slice(row * 23, (row + 1) * 23), 44, 190 + row * 42)
    context.font = '24px sans-serif'; context.fillStyle = '#a5f3fc'; context.fillText(`PDF · Page ${item.page}`, 44, 555)
    const texture = new THREE.CanvasTexture(canvas); texture.colorSpace = THREE.SRGBColorSpace
    const side = new THREE.MeshStandardMaterial({ color: '#ddd6fe', roughness: .7 })
    const cover = new THREE.MeshStandardMaterial({ map: texture, roughness: .55, metalness: .15 })
    const book = new THREE.Mesh(new THREE.BoxGeometry(.72, .94, .12), [side, side, side, side, cover, cover])
    book.position.set(Math.sin(angle) * 2.5, .8 + tier * 1.12, Math.cos(angle) * 2.5)
    book.lookAt(0, book.position.y, 0); book.userData.id = item.id
    books.push(book); scene.add(book)
  }
}
function resetPanorama() {
  if (scene) scene.background = new THREE.Color('#090617')
  panorama?.dispose(); panorama = undefined; panoramaName.value = ''
}
async function loadPanorama(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]; input.value = ''
  if (!file || !scene) return
  error.value = ''
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 12 * 1024 * 1024) { error.value = 'Chọn ảnh JPG/PNG/WebP tối đa 12 MB.'; return }
  let bitmap: ImageBitmap | undefined
  try {
    bitmap = await createImageBitmap(file)
    if (disposed) return
    if (Math.abs(bitmap.width / bitmap.height - 2) > .15) { error.value = 'Ảnh panorama cần tỷ lệ 2:1 (ví dụ 4096 × 2048).'; return }
    const canvas = document.createElement('canvas')
    const maxWidth = Math.min(renderer?.capabilities.maxTextureSize || 4096, 4096)
    const scale = Math.min(1, maxWidth / bitmap.width)
    canvas.width = Math.round(bitmap.width * scale); canvas.height = Math.round(bitmap.height * scale)
    canvas.getContext('2d')!.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
    resetPanorama()
    panorama = new THREE.CanvasTexture(canvas); panorama.mapping = THREE.EquirectangularReflectionMapping; panorama.colorSpace = THREE.SRGBColorSpace
    scene.background = panorama; panoramaName.value = file.name
  } catch { error.value = 'Không đọc được ảnh panorama. Hãy chọn ảnh khác.' }
  finally { bitmap?.close() }
}
function resetView() { if (camera && controls) { camera.position.set(0, 2.2, 6); controls.target.set(0, 1.5, 0); controls.update() } }
onMounted(async () => {
  try {
    if (!host.value) return
    scene = new THREE.Scene(); scene.background = new THREE.Color('#090617')
    camera = new THREE.PerspectiveCamera(60, 1, .1, 60); camera.position.set(0, 2.2, 6)
    renderer = new THREE.WebGLRenderer({ antialias: true }); renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5)); renderer.xr.enabled = true
    host.value.appendChild(renderer.domElement)
    renderer.domElement.setAttribute('aria-label', 'Thư viện PDF 3D; dùng danh sách bên dưới để điều khiển bằng bàn phím')
    renderer.domElement.addEventListener('pointerdown', pointerDown); renderer.domElement.addEventListener('pointerup', pick)
    renderer.domElement.addEventListener('webglcontextlost', event => { event.preventDefault(); error.value = 'Đồ họa 3D bị gián đoạn. Bạn vẫn có thể chọn tài liệu bên dưới.' })
    controls = new OrbitControls(camera, renderer.domElement); controls.target.set(0, 1.5, 0); controls.enableDamping = true; controls.minDistance = 1; controls.maxDistance = 10; controls.maxPolarAngle = Math.PI * .85
    scene.add(new THREE.HemisphereLight('#e9d5ff', '#1e1b4b', 3))
    const light = new THREE.PointLight('#a78bfa', 35); light.position.set(0, 4, 1); scene.add(light)
    const floor = new THREE.Mesh(new THREE.CylinderGeometry(3.7, 3.7, .08, 64), new THREE.MeshStandardMaterial({ color: '#17142f', metalness: .5, roughness: .5 })); scene.add(floor)
    for (let i = 0; i < 3; i++) {
      const ring = new THREE.Mesh(new THREE.TorusGeometry(2.5, .025, 8, 96), new THREE.MeshBasicMaterial({ color: ['#a78bfa', '#818cf8', '#22d3ee'][i] })); ring.rotation.x = Math.PI / 2; ring.position.y = .3 + i * 1.12; scene.add(ring)
    }
    const positions = new Float32Array(450)
    for (let i = 0; i < 150; i++) { const angle = i * 2.39996; positions[i * 3] = Math.cos(angle) * (7 + i % 5); positions[i * 3 + 1] = (i % 13) * .6; positions[i * 3 + 2] = Math.sin(angle) * (7 + i % 5) }
    const geometry = new THREE.BufferGeometry(); geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3))
    scene.add(new THREE.Points(geometry, new THREE.PointsMaterial({ color: '#c4b5fd', size: .035 })))
    buildBooks()
    observer = new ResizeObserver(() => { if (!host.value || !renderer || !camera) return; const { width, height } = host.value.getBoundingClientRect(); if (!width || !height) return; camera.aspect = width / height; camera.updateProjectionMatrix(); renderer.setSize(width, height) }); observer.observe(host.value)
    renderer.setAnimationLoop(() => { if (!renderer || !scene || !camera || (document.hidden && !renderer.xr.isPresenting)) return; if (!renderer.xr.isPresenting) controls?.update(); renderer.render(scene, camera) })
    const xr = (navigator as Navigator & { xr?: { isSessionSupported: (mode: string) => Promise<boolean> } }).xr
    vrAvailable.value = !!xr && await xr.isSessionSupported('immersive-vr').catch(() => false)
    if (!disposed && vrAvailable.value && xrHost.value) {
      xrHost.value.appendChild(VRButton.createButton(renderer))
      for (let i = 0; i < 2; i++) {
        const controller = renderer.xr.getController(i)
        const line = new THREE.Line(new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(), new THREE.Vector3(0, 0, -1)]), new THREE.LineBasicMaterial({ color: '#22d3ee' })); line.scale.z = 6; controller.add(line); scene.add(controller)
        controller.addEventListener('select', () => {
          const rotation = new THREE.Matrix4().extractRotation(controller.matrixWorld)
          raycaster.ray.origin.setFromMatrixPosition(controller.matrixWorld); raycaster.ray.direction.set(0, 0, -1).applyMatrix4(rotation)
          const hit = raycaster.intersectObjects(books)[0]
          if (hit?.object.userData.id) emit('select', hit.object.userData.id)
        })
      }
    }
  } catch { error.value = 'Thiết bị chưa mở được WebGL. Bạn vẫn có thể dùng danh sách tài liệu bên dưới.' }
})
watch(() => props.items, buildBooks, { deep: true })
onBeforeUnmount(() => {
  disposed = true; observer?.disconnect(); controls?.dispose()
  renderer?.setAnimationLoop(null)
  const session = renderer?.xr.getSession(); if (session) void session.end().catch(() => {})
  if (scene) { scene.traverse(node => { if (node instanceof THREE.Line) { node.geometry.dispose(); (node.material as THREE.Material).dispose() } }); free(scene) }
  panorama?.dispose(); renderer?.dispose(); renderer?.domElement.remove(); xrHost.value?.replaceChildren()
})
</script>
<template>
  <section>
    <div ref="host" class="h-[55dvh] min-h-80 overflow-hidden rounded-2xl border border-violet-500/30 bg-[#090617]" />
    <div class="mt-4 flex flex-wrap items-center gap-3">
      <UButton color="gray" icon="i-lucide-rotate-ccw" @click="resetView">Đặt lại góc nhìn</UButton>
      <label class="pf-button cursor-pointer">Nền panorama 360°<input type="file" accept="image/jpeg,image/png,image/webp" class="sr-only" @change="loadPanorama" /></label>
      <UButton v-if="panoramaName" color="gray" @click="resetPanorama">Xóa nền</UButton>
      <div ref="xrHost" class="relative min-w-36 [&>button]:!static [&>button]:!w-auto [&>button]:!translate-x-0" />
    </div>
    <p class="pf-muted mt-3 text-sm">Kéo để xoay 360°, cuộn hoặc chụm để zoom. Bấm một cuốn sách để chọn. {{ vrAvailable ? 'Có thể vào VR bằng kính hỗ trợ WebXR.' : 'VR cần kính và trình duyệt hỗ trợ WebXR; chế độ 3D vẫn dùng bình thường.' }}</p>
    <p class="pf-muted mt-2 text-xs">Ảnh panorama tỷ lệ 2:1 được xử lý trên thiết bị, không tải lên server. Không gian hiển thị tối đa 24 cuốn; danh sách bên dưới có toàn bộ tài liệu.</p>
    <p v-if="error" role="alert" class="mt-3 text-sm text-rose-400">{{ error }}</p>
  </section>
</template>

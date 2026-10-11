import { gsap } from 'gsap'

export function useLandingMotion(root: Ref<HTMLElement | null>) {
  let context: gsap.Context | undefined
  let observer: IntersectionObserver | undefined
  let frame = 0
  const pointer = reactive({ x: 50, y: 25 })
  const reduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches
  onMounted(() => {
    if (!root.value || reduced()) return
    context = gsap.context(() => {
      gsap.from('.hero-line', { y: 28, opacity: 0, stagger: .13, duration: .9, ease: 'power3.out' })
      gsap.from('.hero-demo', { y: 35, opacity: 0, duration: 1, delay: .2, ease: 'power3.out' })
      gsap.to('.floating-pdf', { y: -12, rotation: '+=5', duration: 3.5, stagger: .5, repeat: -1, yoyo: true, ease: 'sine.inOut' })
      gsap.to('.floating-feature', { y: -7, duration: 3, stagger: .4, repeat: -1, yoyo: true, ease: 'sine.inOut' })
      gsap.to('.ai-bestie', { y: -7, duration: 2.7, repeat: -1, yoyo: true, ease: 'sine.inOut' })
      observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
          if (!entry.isIntersecting) return
          context?.add(() => gsap.fromTo(entry.target, { y: 25, opacity: 0 }, { y: 0, opacity: 1, duration: .7, ease: 'power2.out' }))
          observer?.unobserve(entry.target)
        })
      }, { threshold: .12 })
      root.value?.querySelectorAll('.scroll-reveal').forEach(element => observer?.observe(element))
    }, root.value)
  })
  function move(event: PointerEvent) {
    if (event.pointerType !== 'mouse' || reduced()) return
    cancelAnimationFrame(frame)
    const element = event.currentTarget as HTMLElement
    const rect = element.getBoundingClientRect()
    frame = requestAnimationFrame(() => {
      pointer.x = (event.clientX - rect.left) / rect.width * 100
      pointer.y = (event.clientY - rect.top) / rect.height * 100
    })
  }
  function magnet(event: PointerEvent) {
    if (event.pointerType !== 'mouse' || reduced()) return
    const button = event.currentTarget as HTMLElement
    const rect = button.getBoundingClientRect()
    button.style.transform = `translate(${(event.clientX - rect.left - rect.width / 2) * .08}px, ${(event.clientY - rect.top - rect.height / 2) * .12}px)`
  }
  function release(event: PointerEvent) { (event.currentTarget as HTMLElement).style.transform = '' }
  onBeforeUnmount(() => { cancelAnimationFrame(frame); observer?.disconnect(); context?.revert() })
  return { pointer, move, magnet, release }
}

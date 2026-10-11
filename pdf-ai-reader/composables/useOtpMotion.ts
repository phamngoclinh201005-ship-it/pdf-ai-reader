import { gsap } from 'gsap'

export function useOtpMotion(root: Ref<HTMLElement | null>) {
  let context: gsap.Context | undefined
  let media: gsap.MatchMedia | undefined
  let successTimeline: gsap.core.Timeline | undefined
  let settle: (() => void) | undefined
  const reduced = () => import.meta.client && window.matchMedia('(prefers-reduced-motion: reduce)').matches
  onMounted(() => {
    if (!root.value) return
    context = gsap.context(() => {
      media = gsap.matchMedia()
      media.add('(prefers-reduced-motion: no-preference)', () => {
        gsap.from('.story-line', { y: 36, opacity: 0, stagger: .12, duration: .9, ease: 'power3.out' })
        gsap.from('.otp-panel', { scale: .92, y: 24, opacity: 0, duration: 1, delay: .2, ease: 'back.out(1.3)' })
        gsap.from('.universe-art', { y: 40, opacity: 0, duration: 1.2, delay: .3 })
        gsap.from('.orbit-document', { x: i => i % 2 ? 70 : -70, y: -30, opacity: 0, stagger: .15, duration: 1, delay: .5 })
        gsap.from('.robot-mascot', { y: 50, scale: .8, opacity: 0, duration: 1, delay: .7, ease: 'bounce.out' })
        gsap.to('.orbit-document', { y: '+=12', rotation: '+=4', stagger: .4, duration: 3, repeat: -1, yoyo: true, ease: 'sine.inOut' })
        gsap.to('.robot-mascot', { y: -9, duration: 2.5, repeat: -1, yoyo: true, ease: 'sine.inOut' })
      })
    }, root.value)
  })
  function shake() {
    const digits = root.value?.querySelector('.otp-digits')
    if (digits && !reduced()) gsap.fromTo(digits, { x: 0 }, { keyframes: [{ x: -8 }, { x: 8 }, { x: -5 }, { x: 5 }, { x: 0 }], duration: .4 })
  }
  function success() {
    return new Promise<void>(resolve => {
      settle = resolve
      const card = root.value?.querySelector('.otp-panel')
      const digits = root.value?.querySelectorAll('.otp-digits input')
      successTimeline = gsap.timeline({ onComplete: () => { settle = undefined; resolve() } })
      if (card && digits && !reduced()) {
        successTimeline.to(digits, { scale: 1.06, stagger: .04, duration: .2 })
          .to(digits, { scale: 1, duration: .2 })
          .to(card, { boxShadow: '0 0 100px #8b5cf660, 0 0 40px #6ee7d020', duration: .4 }, 0)
      }
      successTimeline.to({}, { duration: reduced() ? .15 : 1.3 }, 0)
    })
  }
  onBeforeUnmount(() => {
    successTimeline?.kill(); settle?.(); media?.revert(); context?.revert()
    if (root.value) gsap.killTweensOf(root.value.querySelector('.otp-digits'))
  })
  return { shake, success }
}

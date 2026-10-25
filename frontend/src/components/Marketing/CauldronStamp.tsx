export default function CauldronStamp({ filled }: { filled: boolean }) {
  return <svg viewBox="0 0 64 64" aria-hidden="true" focusable="false" className={`cauldron-stamp${filled ? ' is-filled' : ''}`}>
    <g fill="none" stroke="currentColor" strokeWidth="2.8" strokeLinecap="round" strokeLinejoin="round">
      <path className="cauldron-steam" d="M24 16c-5-5 5-6 0-12M34 14c-5-5 5-6 1-11M43 16c-4-4 4-6 1-10" />
      <path d="M15 29c-12-5-15 13-4 15M49 29c12-5 15 13 4 15" />
      <path className="cauldron-body" d="M16 25c-3 7-5 14-3 20 2 8 10 11 19 11s17-3 19-11c2-6 0-13-3-20Z" />
      <ellipse className="cauldron-rim" cx="32" cy="24" rx="19" ry="5" />
      <path d="m20 54-3 6M44 54l3 6" />
      <path className="cauldron-spark" d="m32 34 2.5 5.5L40 42l-5.5 2.5L32 50l-2.5-5.5L24 42l5.5-2.5Z" />
    </g>
  </svg>;
}

const speakers = new Set<object>();
const listeners = new Set<(busy: boolean) => void>();

export const isWalkieAudioBusy = () => speakers.size > 0;

export function setWalkieAudioBusy(source: object, busy: boolean) {
  const wasBusy = isWalkieAudioBusy();
  if (busy) speakers.add(source); else speakers.delete(source);
  if (wasBusy !== isWalkieAudioBusy()) {
    for (const listener of listeners) listener(isWalkieAudioBusy());
  }
}

export function subscribeWalkieAudioPriority(listener: (busy: boolean) => void) {
  listeners.add(listener);
  return () => { listeners.delete(listener); };
}

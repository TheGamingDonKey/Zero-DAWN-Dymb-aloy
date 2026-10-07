"""Check the actual exported acknowledgement, not the generator's implementation."""
from pathlib import Path
import array, json, math, wave

root = Path(__file__).resolve().parents[2]
path = root / 'FocusCore/Assets/FocusCore/Art/FocusChirp.wav'
with wave.open(str(path), 'rb') as clip:
    rate, channels, width = clip.getframerate(), clip.getnchannels(), clip.getsampwidth()
    frames = clip.getnframes()
    assert channels == 1 and width == 2, 'Expected mono PCM16 acknowledgement'
    samples = array.array('h', clip.readframes(frames))
values = [s / 32768 for s in samples]
duration = frames / rate
peak = max(map(abs, values))
mean = sum(values) / frames
def rms(part):
    return math.sqrt(sum(s*s for s in part) / len(part))
tail = rms(values[-int(rate*.02):])
maximum_step = max(abs(b-a) for a,b in zip(values, values[1:]))
assert .65 <= duration <= 1.25, 'Soft body and tail must fit within the finite scan'
assert 0.1 < peak <= .5, 'Quiet asset with headroom; no clipping'
assert abs(mean) < .001, 'No significant DC offset'
assert abs(values[0]) < .0001 and abs(values[-1]) < .0001, 'No click at clip boundaries'
assert tail < .003, 'Tail must taper before the clip ends'
assert maximum_step < .15, 'No isolated sample discontinuity'
result = dict(duration_seconds=duration, sample_rate=rate, channels=channels,
              peak=peak, rms=rms(values), dc=mean, tail_rms=tail,
              maximum_sample_step=maximum_step, passed=True,
              perceptual_match_verified=False)
evidence = root / '.artifacts/research/audio-quality.json'
evidence.parent.mkdir(parents=True, exist_ok=True)
evidence.write_text(json.dumps(result, indent=2))
print(json.dumps(result))

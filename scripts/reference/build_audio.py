"""Original soft acknowledgement; authored parameters, not Guerrilla's recipe.

Airy filtered noise, rising resonance, sparse glass-like partials, quiet low
body and diffuse echoes. No reference recording is used as input.
"""
from pathlib import Path
import math, random, struct, wave
root = Path(__file__).resolve().parents[2]
out = root / 'FocusCore/Assets/FocusCore/Art/FocusChirp.wav'
rate, duration = 48000, .94
rng = random.Random(7303)
samples = []
low, high, phase, body_phase = 0., 0., 0., 0.
for i in range(round(rate*duration)):
    t = i/rate
    attack = math.sin(min(1., t/.016)*math.pi/2)**2
    release = min(1., max(0., (duration-t)/.12))**2
    noise = rng.uniform(-1, 1)
    high += .38*(noise-high)
    low += .045*(noise-low)
    air = (high-low)*.14*math.exp(-t/.21)
    frequency = 820 + 740*(1-math.exp(-t/.075))
    phase += 2*math.pi*frequency/rate
    resonance = .24*math.exp(-t/.135)*math.sin(phase + .32*math.sin(phase*1.49))
    glass = sum(gain*math.exp(-t/decay)*math.sin(2*math.pi*hz*t)
                for hz,gain,decay in [(2117,.055,.10),(2879,.026,.072),(1313,.045,.24)])
    body_phase += 2*math.pi*(170+110*math.exp(-t/.11))/rate
    body = .036*math.exp(-t/.17)*math.sin(body_phase)
    samples.append(attack*release*(air+resonance+glass+body))
dry = samples[:]
for delay,gain in [(.031,.11),(.053,.08),(.089,.055),(.143,.032)]:
    offset = round(delay*rate)
    for i in range(offset,len(samples)):
        samples[i] += gain*dry[i-offset]
dc = sum(samples)/len(samples)
for i in range(len(samples)):
    window = min(1., i/(rate*.008), (len(samples)-1-i)/(rate*.1))**2
    samples[i] = (samples[i]-dc)*window
scale = .44/max(map(abs,samples))
with wave.open(str(out),'wb') as clip:
    clip.setnchannels(1); clip.setsampwidth(2); clip.setframerate(rate)
    clip.writeframes(b''.join(struct.pack('<h',round(v*scale*32767)) for v in samples))
print(dict(file=str(out),duration_seconds=duration,sample_rate=rate,original=True))

"""Generate our original scan acknowledgement; no Horizon audio is copied."""
from pathlib import Path
import math, struct, wave
root=Path(__file__).resolve().parents[2]
out=root/'FocusCore/Assets/FocusCore/Art/FocusChirp.wav'
rate=44100; duration=.22; frames=[]
for i in range(round(rate*duration)):
    t=i/rate
    envelope=min(1,t/.012)*min(1,(duration-t)/.04)
    # Linear rise, integrated frequency gives a continuous phase.
    phase=2*math.pi*(300*t+0.5*(600/duration)*t*t)
    value=.5*envelope*(.82*math.sin(phase)+.18*math.sin(phase*2))
    frames.append(struct.pack('<h',round(value*32767)))
with wave.open(str(out),'wb') as audio:
    audio.setnchannels(1);audio.setsampwidth(2);audio.setframerate(rate);audio.writeframes(b''.join(frames))
with wave.open(str(out),'rb') as audio:
    assert audio.getnchannels()==1 and audio.getnframes()==9702
print({'file':str(out),'duration_seconds':duration,'sample_rate':rate,'original':True})

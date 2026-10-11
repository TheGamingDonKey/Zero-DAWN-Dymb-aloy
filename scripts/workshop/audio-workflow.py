"""Stage and measure the original Focus acknowledgement without changing Unity."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path
import platform
import struct
import sys
import wave

ROOT = Path(__file__).resolve().parents[2]
GENERATOR = ROOT / 'scripts/reference/build_audio.py'
SOURCE = ROOT / 'FocusCore/Assets/FocusCore/Art/FocusChirp.wav'


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def measure_wav(path):
    """Measure the written PCM, using the existing quality thresholds."""
    with wave.open(str(path), 'rb') as clip:
        rate, channels, width = clip.getframerate(), clip.getnchannels(), clip.getsampwidth()
        frames = clip.getnframes()
        if channels != 1 or width != 2 or rate <= 0 or frames < 2:
            raise ValueError('Expected nonempty mono PCM16 acknowledgement')
        data = clip.readframes(frames)
    values = [sample[0] / 32768 for sample in struct.iter_unpack('<h', data)]
    if len(values) != frames:
        raise ValueError('WAV sample data is truncated')
    def rms(part):
        return math.sqrt(sum(sample * sample for sample in part) / len(part))
    duration = frames / rate
    peak = max(map(abs, values))
    dc = sum(values) / frames
    tail = rms(values[-max(1, int(rate * .02)):])
    step = max(abs(b - a) for a, b in zip(values, values[1:]))
    checks = {
        'duration_fits_scan': .65 <= duration <= 1.25,
        'quiet_peak_with_headroom': .1 < peak <= .5,
        'dc_offset': abs(dc) < .001,
        'boundary_taper': abs(values[0]) < .0001 and abs(values[-1]) < .0001,
        'tail_taper': tail < .003,
        'sample_continuity': step < .15,
    }
    return dict(sample_rate=rate, channels=channels, sample_width_bytes=width,
                frames=frames, duration_seconds=duration, peak=peak, rms=rms(values),
                dc=dc, tail_rms=tail, maximum_sample_step=step, checks=checks,
                passed=all(checks.values()))


def stage_audio(stage):
    stage = Path(stage).resolve()
    # Repository output belongs only in ignored artifacts, outside either app.
    if stage.is_relative_to(ROOT) and not stage.is_relative_to(ROOT / '.artifacts'):
        raise ValueError('Inside this checkout, choose a fresh directory under .artifacts')
    if stage.exists():
        raise ValueError(f'Staging directory already exists: {stage}; choose a new path')
    source_before = sha256(SOURCE)
    generator_hash = sha256(GENERATOR)
    stage.mkdir(parents=True, exist_ok=False)
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(GENERATOR.parent))
    from build_audio import build_audio
    output = stage / 'FocusChirp.wav'
    build_audio(output)
    metrics = measure_wav(output)
    staged_hash = sha256(output)
    source_after = sha256(SOURCE)
    result = dict(
        generated_at_utc=datetime.now(timezone.utc).isoformat(),
        python_version=platform.python_version(), generator=str(GENERATOR),
        generator_sha256=generator_hash, source=str(SOURCE), source_sha256=source_before,
        source_sha256_after=source_after, source_unchanged=source_before == source_after,
        staged_wav=str(output), staged_sha256=staged_hash,
        byte_identical_to_source=staged_hash == source_before,
        comparison_status='byte-identical' if staged_hash == source_before else 'discrepancy; source retained',
        metrics=metrics, original_synthesis=True, perceptual_match_verified=False,
        listening_verified=False, headset_verified=False,
    )
    (stage / 'audio-report.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--stage', type=Path, required=True,
                        help='New staging directory; existing paths are refused')
    arguments = parser.parse_args()
    try:
        result = stage_audio(arguments.stage)
    except (OSError, ValueError, wave.Error) as error:
        parser.exit(1, f'{error}\n')
    print(json.dumps(result, indent=2))
    return 0 if result['metrics']['passed'] and result['source_unchanged'] else 1


if __name__ == '__main__':
    sys.exit(main())

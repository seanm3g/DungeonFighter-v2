"""Generate GameData/Audio/SFX/combo_finish1..7.wav — combo finisher jingles.

Each jingle is a quick rising arpeggio (root, third, fifth, octave in the major scale) that starts on
the scale note combo step N plays, so a strip's last hit continues the melody instead of jumping back.
Notes are the combo_step1.wav sample resampled to pitch, the same way combo_step2..7 rise a major scale.

    python Scripts/gen-combo-jingles.py
"""
import os
import wave

import numpy as np

SFX = os.path.join(os.path.dirname(__file__), "..", "GameData", "Audio", "SFX")
MAJOR = [0, 2, 4, 5, 7, 9, 11]  # semitones of scale degrees 1..7
ARPEGGIO = [0, 2, 4, 7]          # degree offsets: root, third, fifth, octave
SPACING_S = 0.075                # onset gap between jingle notes
GAP_FADE_S = 0.02                # tail kept on a non-final note after the next one starts


def semitones(degree: int) -> int:
    octave, step = divmod(degree, len(MAJOR))
    return 12 * octave + MAJOR[step]


def read_base():
    with wave.open(os.path.join(SFX, "combo_step1.wav")) as w:
        params = w.getparams()
        raw = np.frombuffer(w.readframes(w.getnframes()), dtype=np.uint8).astype(np.float64) - 128.0
    return params, raw


def pitched(base: np.ndarray, semis: int) -> np.ndarray:
    ratio = 2.0 ** (semis / 12.0)
    n = int(len(base) / ratio)
    return np.interp(np.arange(n) * ratio, np.arange(len(base)), base)


def jingle(base: np.ndarray, rate: int, start_degree: int) -> np.ndarray:
    spacing = int(SPACING_S * rate)
    notes = [pitched(base, semitones(start_degree + off)) for off in ARPEGGIO]
    total = spacing * (len(notes) - 1) + len(notes[-1])
    out = np.zeros(total)
    fade = int(GAP_FADE_S * rate)
    for i, note in enumerate(notes):
        note = note.copy()
        if i < len(notes) - 1:
            keep = min(len(note), spacing + fade)
            note = note[:keep]
            tail = min(fade, keep)
            note[-tail:] *= np.linspace(1.0, 0.0, tail)
        gain = 0.8 if i < len(notes) - 1 else 1.0
        out[i * spacing:i * spacing + len(note)] += note * gain
    peak = np.max(np.abs(out))
    if peak > 127:
        out *= 127 / peak
    return out


def main():
    params, base = read_base()
    for degree in range(7):
        data = jingle(base, params.framerate, degree)
        path = os.path.join(SFX, f"combo_finish{degree + 1}.wav")
        with wave.open(path, "wb") as w:
            w.setparams(params)
            w.writeframes(np.clip(np.round(data + 128), 0, 255).astype(np.uint8).tobytes())
        print(f"{path}: {len(data) / params.framerate:.3f}s")


if __name__ == "__main__":
    main()

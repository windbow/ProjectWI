from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image


TARGET_SIZE = (3840, 2160)
CHUNK_SIZE = (1920, 1080)


def 파일_해시(path: Path) -> str:
    """생성된 배경 파일의 SHA-256 해시를 반환합니다."""
    return hashlib.sha256(path.read_bytes()).hexdigest()


def 화면비_맞춤_크롭(image: Image.Image) -> Image.Image:
    """입력 중앙을 16:9로 잘라 원본 구도의 비율을 보존합니다."""
    target_ratio = TARGET_SIZE[0] / TARGET_SIZE[1]
    source_ratio = image.width / image.height
    if source_ratio > target_ratio:
        width = round(image.height * target_ratio)
        left = (image.width - width) // 2
        return image.crop((left, 0, left + width, image.height))

    height = round(image.width / target_ratio)
    top = (image.height - height) // 2
    return image.crop((0, top, image.width, top + height))


def 완성형_배경과_청크_제작(source_path: Path, output_dir: Path) -> dict:
    """V6 완성형 배경을 4K로 정규화하고 네 청크로 무손실 분할합니다."""
    output_dir.mkdir(parents=True, exist_ok=True)
    with Image.open(source_path) as source:
        cropped = 화면비_맞춤_크롭(source.convert("RGB"))
        master = cropped.resize(TARGET_SIZE, Image.Resampling.LANCZOS)

    master_path = output_dir / "Battle_CompleteArena_CharacterScale_V6_4K.png"
    master.save(master_path, format="PNG", compress_level=6)

    names = ("TL", "TR", "BL", "BR")
    boxes = (
        (0, 0, 1920, 1080),
        (1920, 0, 3840, 1080),
        (0, 1080, 1920, 2160),
        (1920, 1080, 3840, 2160),
    )
    chunk_paths: list[Path] = []
    for name, box in zip(names, boxes):
        path = output_dir / f"Battle_CompleteArena_CharacterScale_V6_{name}.png"
        master.crop(box).save(path, format="PNG", compress_level=6)
        chunk_paths.append(path)

    reconstruction = Image.new("RGB", TARGET_SIZE)
    for path, box in zip(chunk_paths, boxes):
        with Image.open(path) as chunk:
            reconstruction.paste(chunk.convert("RGB"), box[:2])

    report = {
        "source": str(source_path),
        "master": {
            "path": str(master_path),
            "size": list(TARGET_SIZE),
            "sha256": 파일_해시(master_path),
        },
        "chunks": [
            {"path": str(path), "size": list(CHUNK_SIZE), "sha256": 파일_해시(path)}
            for path in chunk_paths
        ],
        "reconstructionPixelPerfect": np.array_equal(
            np.asarray(master), np.asarray(reconstruction)
        ),
        "lighting": "neutral-daylight-baked-static-shadows",
        "layout": "baked-fortress-tents-watchtowers-barricades-open-center",
    }
    report_path = output_dir / "Battle_CompleteArena_CharacterScale_V6.report.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    return report


def main() -> None:
    """현재 V6 ImageGen 원본을 기준으로 전투용 파일을 생성합니다."""
    script_path = Path(__file__).resolve()
    output_dir = script_path.parent.parent
    source_path = script_path.parent / "Battle_CompleteArena_CharacterScale_V6_ImageGen.png"
    print(
        json.dumps(
            완성형_배경과_청크_제작(source_path, output_dir),
            ensure_ascii=False,
            indent=2,
        )
    )


if __name__ == "__main__":
    main()

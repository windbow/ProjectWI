from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
HEADER_PATH = ROOT / "Assets" / "Resources" / "UI" / "Generated" / "popup_header.png"


def rebuild_popup_header() -> None:
    """기존 금속 외곽을 보존하고 중앙 양피지 질감을 좌우 끝까지 확장합니다."""
    source = Image.open(HEADER_PATH).convert("RGBA")
    width, height = source.size
    result = source.copy()

    parchment = source.crop((180, 30, width - 180, height - 30))
    parchment = parchment.resize((width - 30, height - 38), Image.Resampling.BICUBIC)
    result.paste(parchment, (15, 19))

    result.paste(source.crop((0, 0, width, 19)), (0, 0))
    result.paste(source.crop((0, height - 19, width, height)), (0, height - 19))
    result.paste(source.crop((0, 0, 15, height)), (0, 0))
    result.paste(source.crop((width - 15, 0, width, height)), (width - 15, 0))

    corner_width = 42
    corner_height = 24
    result.paste(source.crop((0, 0, corner_width, corner_height)), (0, 0))
    result.paste(source.crop((width - corner_width, 0, width, corner_height)), (width - corner_width, 0))
    result.paste(source.crop((0, height - corner_height, corner_width, height)), (0, height - corner_height))
    result.paste(
        source.crop((width - corner_width, height - corner_height, width, height)),
        (width - corner_width, height - corner_height),
    )

    result.save(HEADER_PATH, optimize=True)


if __name__ == "__main__":
    rebuild_popup_header()

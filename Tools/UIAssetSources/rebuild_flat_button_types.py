from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ASSET_DIR = ROOT / "Assets" / "Resources" / "UI" / "Generated"


def rebuild_button(path: Path) -> None:
    """기존 중앙 질감을 유지하면서 버튼을 얇은 단일 테두리 형태로 다시 만듭니다."""
    source = Image.open(path).convert("RGBA")
    width, height = source.size
    interior = source.crop((18, 16, width - 18, height - 16))
    canvas = interior.resize((width, height), Image.Resampling.BICUBIC)

    alpha = Image.new("L", (width, height), 255)
    alpha_draw = ImageDraw.Draw(alpha)
    alpha_draw.polygon([(0, 0), (1, 0), (0, 1)], fill=0)
    alpha_draw.polygon([(width - 2, 0), (width - 1, 0), (width - 1, 1)], fill=0)
    alpha_draw.polygon([(0, height - 2), (0, height - 1), (1, height - 1)], fill=0)
    alpha_draw.polygon([(width - 1, height - 2), (width - 1, height - 1), (width - 2, height - 1)], fill=0)
    border = ImageDraw.Draw(canvas)
    outline = [(2, 0), (width - 3, 0), (width - 1, 2), (width - 1, height - 3),
               (width - 3, height - 1), (2, height - 1), (0, height - 3), (0, 2), (2, 0)]
    border.line(outline, fill=(143, 147, 154, 255), width=2, joint="curve")
    canvas.putalpha(alpha)
    canvas.save(path, optimize=True)


def main() -> None:
    """A/B 버튼을 같은 형상으로 재작성하되 각 버튼의 중앙 색상은 유지합니다."""
    rebuild_button(ASSET_DIR / "button_flat_normal.png")
    rebuild_button(ASSET_DIR / "button_flat_primary.png")


if __name__ == "__main__":
    main()

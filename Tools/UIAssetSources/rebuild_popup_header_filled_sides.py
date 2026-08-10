from pathlib import Path

from PIL import Image, ImageChops


ROOT = Path(__file__).resolve().parents[2]
REFERENCE_PATH = Path(r"C:\Users\windb\AppData\Local\Temp\codex-clipboard-3512db58-553b-4f92-b709-a4564802c821.png")
SOURCE_PATH = ROOT / "Tools" / "UIAssetSources" / "popup_header_selected_source.png"
HEADER_PATH = ROOT / "Assets" / "Resources" / "UI" / "Generated" / "popup_header.png"


def crop_black_margin(image: Image.Image) -> Image.Image:
    """첨부 이미지의 검은 바깥 여백만 찾아 제거합니다."""
    rgb = image.convert("RGB")
    threshold = rgb.point(lambda value: 255 if value > 12 else 0)
    bounds = ImageChops.difference(threshold, Image.new("RGB", rgb.size)).getbbox()
    if bounds is None:
        raise ValueError("팝업 헤더 영역을 찾을 수 없습니다.")
    return image.crop(bounds)


def rebuild_popup_header() -> None:
    """사용자가 선택한 헤더를 보존하고 9-Slice용 해상도로 변환합니다."""
    input_path = REFERENCE_PATH if REFERENCE_PATH.exists() else SOURCE_PATH
    selected = crop_black_margin(Image.open(input_path).convert("RGBA"))
    selected.save(SOURCE_PATH, optimize=True)

    target_width = 1024
    target_height = round(selected.height * target_width / selected.width)
    header = selected.resize((target_width, target_height), Image.Resampling.LANCZOS)
    header.save(HEADER_PATH, optimize=True)


if __name__ == "__main__":
    rebuild_popup_header()

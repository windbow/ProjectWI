from pathlib import Path
from PIL import Image


ROOT = Path(r"C:/Fork/ProjectWI")
OUTPUT = ROOT / "Assets/Resources/UI/Generated"
SOURCE_CACHE = ROOT / "Tools/UIAssetSources"
ICON_SOURCE = Path(r"C:/Users/windb/.codex/generated_images/019fb2ba-4a17-7741-bcd2-fdd2ab4190e1/exec-bd835690-d779-447e-8c32-fdf29e9c36b5.png")
BUTTON_SOURCE = Path(r"C:/Users/windb/.codex/generated_images/019fb2ba-4a17-7741-bcd2-fdd2ab4190e1/exec-8be65276-9b7d-40a7-b5d6-6b8be206d928.png")
PANEL_SOURCE = Path(r"C:/Users/windb/.codex/generated_images/019fb2ba-4a17-7741-bcd2-fdd2ab4190e1/exec-fc68af57-548e-42b6-b12e-83def0bb5533.png")
HEADER_SOURCE = Path(r"C:/Users/windb/.codex/generated_images/019fb2ba-4a17-7741-bcd2-fdd2ab4190e1/exec-eb4f83cf-7b50-4c2f-8cc8-280e27e48518.png")
ICON_CLEAN = SOURCE_CACHE / "icon_atlas_clean.png"
BUTTON_CLEAN = SOURCE_CACHE / "button_atlas_clean.png"
CASTLE_STAT_CLEAN = SOURCE_CACHE / "castle_stat_atlas_clean.png"
CASTLE_SLOT_CLEAN = SOURCE_CACHE / "castle_slot_atlas_clean.png"


def remove_magenta(image: Image.Image) -> Image.Image:
    """생성 시트의 자홍색 키 배경을 부드러운 알파로 변환합니다."""
    rgba = image.convert("RGBA")
    pixels = []
    for red, green, blue, _ in rgba.getdata():
        distance = abs(255 - red) + green + abs(255 - blue)
        alpha = 0 if distance < 45 else min(255, max(0, (distance - 45) * 5))
        pixels.append((red, green, blue, alpha))
    rgba.putdata(pixels)
    return rgba


def trim_and_square(image: Image.Image, size: int = 128) -> Image.Image:
    """아이콘의 불투명 영역을 기준으로 자르고 정사각 안전 여백을 추가합니다."""
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        return Image.new("RGBA", (size, size))
    cropped = image.crop(bbox)
    edge = max(cropped.size)
    canvas = Image.new("RGBA", (edge, edge))
    canvas.alpha_composite(cropped, ((edge - cropped.width) // 2, (edge - cropped.height) // 2))
    padded = Image.new("RGBA", (edge + edge // 5, edge + edge // 5))
    padded.alpha_composite(canvas, ((padded.width - edge) // 2, (padded.height - edge) // 2))
    return padded.resize((size, size), Image.Resampling.LANCZOS)


def extract_icons() -> None:
    """4×2 아이콘 시트를 메뉴별 독립 PNG로 분리합니다."""
    source = Image.open(ICON_CLEAN).convert("RGBA") if ICON_CLEAN.exists() else remove_magenta(Image.open(ICON_SOURCE))
    names = ["military", "heroes", "diplomacy", "scheme", "research", "council", "report", "turn"]
    cell_width = source.width // 4
    cell_height = source.height // 2
    for index, name in enumerate(names):
        column = index % 4
        row = index // 4
        cell = source.crop((column * cell_width, row * cell_height,
                            (column + 1) * cell_width, (row + 1) * cell_height))
        trim_and_square(cell).save(OUTPUT / f"icon_{name}.png")


def extract_buttons() -> None:
    """세로 버튼 시트를 일반·주요·위험 버튼 텍스처로 분리합니다."""
    source = Image.open(BUTTON_CLEAN).convert("RGBA") if BUTTON_CLEAN.exists() else remove_magenta(Image.open(BUTTON_SOURCE))
    names = ["button_normal", "button_primary", "button_danger"]
    row_height = source.height // 3
    for row, name in enumerate(names):
        cell = source.crop((0, row * row_height, source.width, (row + 1) * row_height))
        bbox = cell.getchannel("A").getbbox()
        if bbox is None:
            continue
        cropped = cell.crop(bbox)
        cropped.resize((512, 128), Image.Resampling.LANCZOS).save(OUTPUT / f"{name}.png")


def prepare_panel() -> None:
    """팝업 프레임 원본을 런타임 사용 크기로 축소해 저장합니다."""
    panel = Image.open(PANEL_SOURCE).convert("RGBA")
    panel.resize((1024, 683), Image.Resampling.LANCZOS).save(OUTPUT / "popup_panel.png")


def prepare_header() -> None:
    """흰 외곽 여백을 제거하고 공통 팝업 제목 배너 크기로 변환합니다."""
    header = Image.open(HEADER_SOURCE).convert("RGBA")
    rgb = header.convert("RGB")
    active_rows = []
    for y in range(rgb.height):
        dark_pixels = sum(1 for x in range(0, rgb.width, 4) if max(rgb.getpixel((x, y))) < 225)
        if dark_pixels > rgb.width // 40:
            active_rows.append(y)
    top = max(0, min(active_rows) - 4)
    bottom = min(header.height, max(active_rows) + 5)
    cropped = header.crop((0, top, header.width, bottom))
    cropped.resize((1024, 160), Image.Resampling.LANCZOS).save(OUTPUT / "popup_header.png")


def extract_castle_stats() -> None:
    """2x2 성 수치 카드 시트를 번영·기술·치안·방어 카드로 분리합니다."""
    source = Image.open(CASTLE_STAT_CLEAN).convert("RGBA")
    cards = {
        "castle_stat_prosperity": (20, 165, 752, 480),
        "castle_stat_technology": (782, 165, 1516, 480),
        "castle_stat_stability": (20, 535, 752, 850),
        "castle_stat_defense": (782, 535, 1516, 850),
    }
    for name, bounds in cards.items():
        source.crop(bounds).resize((512, 176), Image.Resampling.LANCZOS).save(OUTPUT / f"{name}.png")


def extract_castle_slots() -> None:
    """영웅과 특화 시설 슬롯 시트를 독립적인 가로형 카드로 분리합니다."""
    source = Image.open(CASTLE_SLOT_CLEAN).convert("RGBA")
    cards = {
        "hero_slot_card": (45, 85, 1490, 495),
        "facility_slot_card": (45, 545, 1490, 950),
    }
    for name, bounds in cards.items():
        source.crop(bounds).resize((640, 180), Image.Resampling.LANCZOS).save(OUTPUT / f"{name}.png")


def main() -> None:
    """생성 원본을 Unity Resources 폴더의 최종 UI 에셋으로 변환합니다."""
    OUTPUT.mkdir(parents=True, exist_ok=True)
    extract_icons()
    extract_buttons()
    prepare_panel()
    prepare_header()
    extract_castle_stats()
    extract_castle_slots()


if __name__ == "__main__":
    main()

export function competitionImageUrl(image: string | null): string | undefined {
    return image ? `/competition-images/${image}` : undefined;
}

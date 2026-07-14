export function crestUrl(image: string | null): string | undefined {
    return image ? `/team-crests/${image}` : undefined;
}

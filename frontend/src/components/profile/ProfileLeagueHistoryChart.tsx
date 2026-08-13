import { useRef, useState } from "react";
import { Box, Heading, Text } from "@chakra-ui/react";
import type { UserCompetitionLeagueHistoryItem } from "../../services/profile-service";
import { Panel } from "../ui/panel";

const WIDTH = 600;
const HEIGHT = 220;
const PAD_LEFT = 32;
const PAD_RIGHT = 12;
const PAD_TOP = 12;
const PAD_BOTTOM = 28;

function formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, { day: "numeric", month: "short" });
}

// Thins a list of indices down to ones that fit without crowding, always keeping the first/last.
function pickLabelIndices(count: number, maxLabels: number): number[] {
    if (count <= maxLabels) return Array.from({ length: count }, (_, i) => i);

    const step = (count - 1) / (maxLabels - 1);
    const indices = new Set<number>();
    for (let i = 0; i < maxLabels; i++) {
        indices.add(Math.round(i * step));
    }
    return Array.from(indices).sort((a, b) => a - b);
}

export function ProfileLeagueHistoryChart({ history, worstPosition }: {
    history: UserCompetitionLeagueHistoryItem[];
    worstPosition: number;
}) {
    const svgRef = useRef<SVGSVGElement>(null);
    const [hoverIndex, setHoverIndex] = useState<number | null>(null);

    if (history.length === 0) {
        return (
            <Panel accent hoverLift>
                <Heading size="md" mb={2}>League Position by Date</Heading>
                <Text color="fg.muted">No history yet</Text>
            </Panel>
        );
    }

    // Position 1 is best, so the Y scale is inverted: 1 maps to the top, worstPosition to the bottom.
    const maxPosition = Math.max(worstPosition, ...history.map((h) => h.leaguePosition));
    const plotWidth = WIDTH - PAD_LEFT - PAD_RIGHT;
    const plotHeight = HEIGHT - PAD_TOP - PAD_BOTTOM;

    const xForIndex = (i: number) => history.length === 1
        ? PAD_LEFT + plotWidth / 2
        : PAD_LEFT + (i / (history.length - 1)) * plotWidth;

    const yForPosition = (position: number) =>
        maxPosition === 1
            ? PAD_TOP + plotHeight / 2
            : PAD_TOP + ((position - 1) / (maxPosition - 1)) * plotHeight;

    const points = history.map((h, i) => ({ x: xForIndex(i), y: yForPosition(h.leaguePosition), h }));
    const linePath = points.map((p, i) => `${i === 0 ? "M" : "L"} ${p.x},${p.y}`).join(" ");

    // 5 evenly-spaced Y gridlines spanning [1, maxPosition], rounded to whole positions.
    const yTickCount = Math.min(5, maxPosition);
    const yTicks = Array.from({ length: yTickCount }, (_, i) =>
        Math.round(1 + (i / Math.max(1, yTickCount - 1)) * (maxPosition - 1))
    );

    const labelIndices = pickLabelIndices(history.length, 6);

    const handlePointerMove = (e: React.PointerEvent<SVGSVGElement>) => {
        const svg = svgRef.current;
        if (!svg) return;

        const rect = svg.getBoundingClientRect();
        const scaleX = WIDTH / rect.width;
        const localX = (e.clientX - rect.left) * scaleX;

        let nearest = 0;
        let nearestDist = Infinity;
        points.forEach((p, i) => {
            const dist = Math.abs(p.x - localX);
            if (dist < nearestDist) {
                nearestDist = dist;
                nearest = i;
            }
        });
        setHoverIndex(nearest);
    };

    const hovered = hoverIndex !== null ? points[hoverIndex] : null;

    return (
        <Panel accent hoverLift>
            <Heading size="md" mb={2}>League Position by Date</Heading>
            <Box position="relative">
                <svg
                    ref={svgRef}
                    viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
                    width="100%"
                    style={{ display: "block", overflow: "visible" }}
                    onPointerMove={handlePointerMove}
                    onPointerLeave={() => setHoverIndex(null)}
                >
                    {/* Gridlines + Y-axis labels (recessive, muted) */}
                    <g style={{ color: "var(--chakra-colors-fg-muted)" }}>
                        {yTicks.map((tick) => (
                            <g key={tick}>
                                <line
                                    x1={PAD_LEFT} x2={WIDTH - PAD_RIGHT}
                                    y1={yForPosition(tick)} y2={yForPosition(tick)}
                                    stroke="currentColor" strokeOpacity={0.15} strokeWidth={1}
                                />
                                <text x={PAD_LEFT - 6} y={yForPosition(tick)} textAnchor="end" dominantBaseline="middle" fontSize={10} fill="currentColor">
                                    {tick}
                                </text>
                            </g>
                        ))}
                        {labelIndices.map((i) => (
                            <text
                                key={i}
                                x={xForIndex(i)}
                                y={HEIGHT - PAD_BOTTOM + 16}
                                textAnchor="middle"
                                fontSize={10}
                                fill="currentColor"
                            >
                                {formatDate(history[i].date)}
                            </text>
                        ))}
                    </g>

                    {/* Crosshair, snapped to the nearest point */}
                    {hovered && (
                        <line
                            x1={hovered.x} x2={hovered.x} y1={PAD_TOP} y2={HEIGHT - PAD_BOTTOM}
                            style={{ color: "var(--chakra-colors-fg-muted)" }}
                            stroke="currentColor" strokeOpacity={0.4} strokeWidth={1}
                        />
                    )}

                    {/* The line itself */}
                    <path
                        d={linePath} fill="none"
                        style={{ color: "var(--chakra-colors-blue-500)" }}
                        stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round"
                    />

                    {/* End marker */}
                    <circle
                        cx={points[points.length - 1].x} cy={points[points.length - 1].y} r={5}
                        style={{ color: "var(--chakra-colors-blue-500)" }} fill="currentColor"
                    />

                    {/* Hover marker */}
                    {hovered && (
                        <circle cx={hovered.x} cy={hovered.y} r={5} style={{ color: "var(--chakra-colors-blue-500)" }} fill="currentColor" />
                    )}
                </svg>

                {hovered && (
                    <Box
                        position="absolute"
                        left={`${(hovered.x / WIDTH) * 100}%`}
                        top={0}
                        transform="translate(-50%, -100%)"
                        bg="bg.panel"
                        borderWidth="1px"
                        rounded="sm"
                        px={2} py={1}
                        pointerEvents="none"
                        fontSize="xs"
                        whiteSpace="nowrap"
                        boxShadow="sm"
                    >
                        <Text fontWeight="bold">Position {hovered.h.leaguePosition}</Text>
                        <Text color="fg.muted">{formatDate(hovered.h.date)}</Text>
                    </Box>
                )}
            </Box>
        </Panel>
    );
}

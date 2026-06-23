import type { Meta, StoryObj } from "@storybook/react-vite";
import { fn } from "storybook/test";
import { Select as Component } from "./Select";

const meta = {
    title: "Components/Select",
    component: Component,
    parameters: {
        layout: "centered"
    },
    args: {
        size: "sm",
        width: "250px",
        placeholder: "",
        items: [
            { label: "Item 1", value: "item1" },
            { label: "Item 2", value: "item2" },
            { label: "Item 3", value: "item3" }
        ],
        value: "",
        onValueChange: fn(),
        allowClear: false
    }
} satisfies Meta<typeof Component>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Select: Story = {};

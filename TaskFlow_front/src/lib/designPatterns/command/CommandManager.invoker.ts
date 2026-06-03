import { Command } from './command.interface';

export class CommandManager {

    private history: Command[] = [];

    async executeCommand(command: Command): Promise<void> {
        await command.execute();
        this.history.push(command);
    }

    async undo(): Promise<void> {
        const last = this.history.pop();

        if (last) {
            await last.undo();
        }
    }
}
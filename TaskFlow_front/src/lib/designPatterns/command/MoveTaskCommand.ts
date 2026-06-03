import { TaskManagerProxy } from '../TaskManagementProxy';
import { Command } from './command.interface';

export class MoveTaskCommand implements Command {

    constructor(
        private taskId: string,
        private oldStatus: string,
        private newStatus: string,
        private projectId: string,
        private receiver: TaskManagerProxy
    ) {}

    async execute(): Promise<void> {
        await this.receiver.updateTask(
            this.projectId,
            this.taskId,
            {
                status: this.newStatus
            }
        );
    }

    async undo(): Promise<void> {
        await this.receiver.updateTask(
            this.projectId,
            this.taskId,
            {
                status: this.oldStatus
            }
        );
    }
}
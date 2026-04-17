namespace Core.MirroringConnection.MSocket;

internal class SocketProtocol
{
	public const byte ID = 0;

	public const byte REQUEST_CONNECT = 1;

	public const byte CONNECT_AGREE = 2;

	public const byte CONTROL_REMOTE = 3;

	public const byte CONTROL_SWIPE = 4;

	public const byte CONTROL_MULTIPLE_SWIPE = 5;

	public const byte CONTROL_ROTATE_RESET = 16;

	public const byte RECEIVE_CONTROL = 6;

	public const byte RECEIVE_FRAME = 7;

	public const byte UPDATE_SIZE = 9;

	public const byte CONNECT_REFUSE = 8;

	public const byte NETWORK_LOW = 17;

	public const byte NETWORK_MEDIUM = 18;

	public const byte NETWORK_HIGH = 19;

	public const byte AUDIO_SUPPORT = 20;

	public const byte Disconnect = 153;
}

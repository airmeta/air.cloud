
1. ��ǰ��������ļ����µ��������Ϊ������ȡ���. ֻ��Ҫ�����api������������������⼴����ɵ���

�������������ֱ��ǻ���Windows �� Linuxƽ̨��д��, ������ʹ��ʱ��Ҫ����������л���ѡ����ʵ��������.

�����Startup.cs�ļ���,����ҪAdd������ķ���,Ȼ����Ϳ�������Ŀ��������������ط�ʹ�������������.


Docker ���ʹ�ô���:
    DockerContainer<DockerContainerInstance> container = new DockerContainer<DockerContainerInstance>();
    await container.Load();
   
IIS ���ʹ�ô���:
	IISContainer<IISContainerInstance> container = new IISContainer<IISContainerInstance>();
	await container.Load();

��ϸ�ο�unit����Ĳ��������÷���

�������¼ƻ��ǽ���ͳһ���������ĵ��÷�ʽ,���Ҽ�һ���ִ���. ����Ŀǰ�İ汾�Ѿ���������󲿷ֵ�������.
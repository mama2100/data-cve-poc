package com.memlastic.converter;

import com.memlastic.exception.DataLimitExceededException;
import org.springframework.http.HttpInputMessage;
import org.springframework.http.HttpOutputMessage;
import org.springframework.http.MediaType;
import org.springframework.http.converter.AbstractHttpMessageConverter;
import org.springframework.lang.Nullable;
import org.springframework.util.StreamUtils;

import java.io.ByteArrayOutputStream;
import java.io.IOException;

/**
 * A converter with improved logic for checking the maximum allowable size of the received file,
 * which converts incoming content into an array of bytes.
 */
public class ByteArrayConverter extends AbstractHttpMessageConverter<byte[]> {
    private final Integer limit;

    public ByteArrayConverter(Integer limit) {
        super(MediaType.APPLICATION_OCTET_STREAM, MediaType.ALL);
        this.limit = limit;
    }


    @Override
    public boolean supports(Class<?> clazz) {
        return byte[].class == clazz;
    }


    @Override
    public byte[] readInternal(Class<? extends byte[]> clazz, HttpInputMessage inputMessage) throws IOException {
        long contentLength = inputMessage.getHeaders().getContentLength();
        if (contentLength > limit) {
            throw new DataLimitExceededException("Exceeding the limit");
        }
        ByteArrayOutputStream bos =
                new ByteArrayOutputStream(contentLength >= 0 ? (int) contentLength : StreamUtils.BUFFER_SIZE);
        StreamUtils.copy(inputMessage.getBody(), bos);
        return bos.toByteArray();
    }

    @Override
    protected Long getContentLength(byte[] bytes, @Nullable MediaType contentType) {
        return (long) bytes.length;
    }

    @Override
    protected void writeInternal(byte[] bytes, HttpOutputMessage outputMessage) throws IOException {
        StreamUtils.copy(bytes, outputMessage.getBody());
    }
}
